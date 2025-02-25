﻿// <copyright file="ItemPowerUpFactory.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence;

/// <summary>
/// The implementation of the item power up factory.
/// </summary>
public class ItemPowerUpFactory : IItemPowerUpFactory
{
    private readonly ILogger<ItemPowerUpFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPowerUpFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ItemPowerUpFactory(ILogger<ItemPowerUpFactory> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public IEnumerable<PowerUpWrapper> GetPowerUps(Item item, AttributeSystem attributeHolder)
    {
        if (item.Definition is null)
        {
            this._logger.LogWarning("Item of slot {itemSlot} ({itemId}) has no definition.", item.ItemSlot, item.GetId());
            yield break;
        }

        if (item.Durability <= 0)
        {
            yield break;
        }

        if (item.ItemSlot < InventoryConstants.FirstEquippableItemSlotIndex || item.ItemSlot > InventoryConstants.LastEquippableItemSlotIndex)
        {
            yield break;
        }

        var isRightWieldWeapon = item.ItemSlot == InventoryConstants.RightHandSlot
            && item.Definition!.BasePowerUpAttributes.Any(pu => pu.TargetAttribute == Stats.DoubleWieldWeaponCount || pu.TargetAttribute == Stats.IsOneHandedStaffEquipped);

        AttributeDefinition? targetAttribute = null;
        foreach (var attribute in item.Definition.BasePowerUpAttributes)
        {
            if (isRightWieldWeapon)
            {
                if (attribute.TargetAttribute == Stats.StaffRise)
                {
                    continue;
                }
                else if (attribute.TargetAttribute == Stats.MinimumPhysBaseDmgByWeapon)
                {
                    targetAttribute = Stats.MinPhysBaseDmgByRightWeapon;
                }
                else if (attribute.TargetAttribute == Stats.MaximumPhysBaseDmgByWeapon)
                {
                    targetAttribute = Stats.MaxPhysBaseDmgByRightWeapon;
                }
                else
                {
                    targetAttribute = null;
                }
            }

            foreach (var powerUp in this.GetBasePowerUpWrappers(item, attributeHolder, attribute, targetAttribute))
            {
                yield return powerUp;
            }
        }

        foreach (var powerUp in this.GetPowerUpsOfItemOptions(item, attributeHolder))
        {
            yield return powerUp;
        }

        if (this.GetPetLevel(item, attributeHolder) is { } petLevel)
        {
            yield return petLevel;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<PowerUpWrapper> GetSetPowerUps(
        IEnumerable<Item> equippedItems,
        AttributeSystem attributeHolder,
        GameConfiguration gameConfiguration)
    {
        var activeItems = equippedItems
            .Where(i => i.Durability > 0)
            .ToList();
        var itemGroups = activeItems
            .SelectMany(i => i.ItemSetGroups)
            .Select(i => i.ItemSetGroup!)
            .Distinct();

        var result = Enumerable.Empty<PowerUpDefinition>();
        var alwaysGroups = activeItems.SelectMany(i => i.Definition!.PossibleItemSetGroups).Where(i => i.AlwaysApplies).Distinct();

        foreach (var group in alwaysGroups.Concat(itemGroups).Distinct())
        {
            var itemsOfGroup = activeItems.Where(i =>
                ((group.AlwaysApplies && i.Definition!.PossibleItemSetGroups.Contains(group))
                 || i.ItemSetGroups.Any(ios => ios.ItemSetGroup == group))
                && (group.SetLevel == 0 || i.Level >= group.SetLevel));
            var setMustBeComplete = group.MinimumItemCount == group.Items.Count;
            if (group.SetLevel > 0 && setMustBeComplete && itemsOfGroup.All(i => i.Level > group.SetLevel))
            {
                // When all items are of higher level and the set bonus is applied when all items are there, another item set group will take care.
                // This should prevent that for example set bonus defense is applied multiple times.
                continue;
            }

            if (group.Options is not { } options)
            {
                this._logger.LogWarning("Options of set {group} is not initialized", group);
                continue;
            }

            var itemCount = group.CountDistinct ? itemsOfGroup.Select(item => item.Definition).Distinct().Count() : itemsOfGroup.Count();
            var setIsComplete = itemCount == group.Items.Count;
            if (setIsComplete)
            {
                // Take all options when the set is complete
                result = result.Concat(
                    options.PossibleOptions
                        .Select(o => o.PowerUpDefinition ?? throw Error.NotInitializedProperty(o, nameof(o.PowerUpDefinition))));
                continue;
            }

            if (itemCount >= group.MinimumItemCount)
            {
                // Take the first n-1 options
                result = result.Concat(options.PossibleOptions.OrderBy(o => o.Number)
                    .Take(itemCount - 1)
                    .Select(o => o.PowerUpDefinition ?? throw Error.NotInitializedProperty(o, nameof(o.PowerUpDefinition))));
            }
        }

        result = result.Concat(this.GetOptionCombinationBonus(activeItems, gameConfiguration));

        return result.SelectMany(p => PowerUpWrapper.CreateByPowerUpDefinition(p, attributeHolder));
    }

    private IEnumerable<PowerUpDefinition> GetOptionCombinationBonus(IEnumerable<Item> activeItems, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration?.ItemOptionCombinationBonuses is null
            || gameConfiguration.ItemOptionCombinationBonuses.Count == 0)
        {
            yield break;
        }

        var activeItemOptions = activeItems.SelectMany(i => i.ItemOptions.Select(o => o.ItemOption ?? throw Error.NotInitializedProperty(o, nameof(o.ItemOption)))).ToList();
        foreach (var combinationBonus in gameConfiguration.ItemOptionCombinationBonuses.Where(c => c.Bonus is { }))
        {
            var remainingOptions = activeItemOptions.ToList<ItemOption>();
            while (this.AreRequiredOptionsFound(combinationBonus, remainingOptions))
            {
                if (combinationBonus.Bonus is not null)
                {
                    yield return combinationBonus.Bonus;
                }
                else
                {
                    this._logger.LogWarning("Bonus of ItemOptionCombinationBonus '{combinationBonusName}' is not initialized, id: {id}", combinationBonus.Description, combinationBonus.GetId());
                }

                if (!combinationBonus.AppliesMultipleTimes)
                {
                    break;
                }
            }
        }
    }

    private bool AreRequiredOptionsFound(ItemOptionCombinationBonus bonus, IList<ItemOption> itemOptions)
    {
        var allMatches = new List<ItemOption>();
        foreach (var requirement in bonus.Requirements)
        {
            var matches = itemOptions
                .Where(o => o.OptionType is not null)
                .Where(o => o.OptionType == requirement.OptionType && o.SubOptionType == requirement.SubOptionType)
                .Take(requirement.MinimumCount)
                .ToList();
            if (matches.Count < requirement.MinimumCount)
            {
                return false;
            }

            allMatches.AddRange(matches);
        }

        allMatches.ForEach(o => itemOptions.Remove(o));
        return true;
    }

    private IEnumerable<PowerUpWrapper> GetBasePowerUpWrappers(Item item, AttributeSystem attributeHolder, ItemBasePowerUpDefinition attribute, AttributeDefinition? targetAttribute = null)
    {
        attribute.ThrowNotInitializedProperty(attribute.BaseValueElement is null, nameof(attribute.BaseValueElement));
        attribute.ThrowNotInitializedProperty(attribute.TargetAttribute is null, nameof(attribute.TargetAttribute));

        var levelBonusElmt = (attribute.BonusPerLevelTable?.BonusPerLevel ?? Enumerable.Empty<LevelBonus>())
            .FirstOrDefault(bonus => bonus.Level == item.Level)?
            .GetAdditionalValueElement(attribute.AggregateType);

        if (levelBonusElmt is null)
        {
            yield return new PowerUpWrapper(attribute.BaseValueElement, targetAttribute ?? attribute.TargetAttribute, attributeHolder);
        }
        else
        {
            yield return new PowerUpWrapper(
                new CombinedElement(attribute.BaseValueElement, levelBonusElmt),
                targetAttribute ?? attribute.TargetAttribute,
                attributeHolder);
        }
    }

    private IEnumerable<PowerUpWrapper> GetPowerUpsOfItemOptions(Item item, AttributeSystem attributeHolder)
    {
        var options = item.ItemOptions;
        if (options is null)
        {
            yield break;
        }

        foreach (var optionLink in options)
        {
            var option = optionLink.ItemOption;
            if (option is null)
            {
                this._logger.LogWarning("Item {item} (id {itemId}) has ItemOptionLink ({optionLinkId}) without option.", item, item.GetId(), optionLink.GetId());
                continue;
            }

            if (item.ItemSlot == InventoryConstants.RightHandSlot && option.PowerUpDefinition?.TargetAttribute == Stats.WizardryBaseDmg)
            {
                // For a RH-wielded staff (MG), its wizardry item option doesn't count (but the others do!)
                continue;
            }

            var level = option.LevelType == LevelType.ItemLevel ? item.Level : optionLink.Level;

            var optionOfLevel = option.LevelDependentOptions?.FirstOrDefault(l => l.Level == level);
            if (optionOfLevel is null && level > 1 && item.Definition!.Skill?.Number != 49) // Dinorant options are an exception
            {
                this._logger.LogWarning("Item {item} (id {itemId}) has IncreasableItemOption ({option}, id {optionId}) with level {level}, but no definition in LevelDependentOptions.", item, item.GetId(), option, option.GetId(), level);
                continue;
            }

            if (optionOfLevel?.RequiredItemLevel > item.Level)
            {
                // Some options (like harmony) although on the item can be inactive.
                continue;
            }

            var powerUp = optionOfLevel?.PowerUpDefinition ?? option.PowerUpDefinition;

            if (powerUp?.Boost is null)
            {
                // Some options are level dependent. If they are at level 0, they might not have any boost yet.
                continue;
            }

            foreach (var wrapper in PowerUpWrapper.CreateByPowerUpDefinition(powerUp, attributeHolder))
            {
                yield return wrapper;
            }
        }

        foreach (var powerUpWrapper in this.CreateExcellentAndAncientBasePowerUpWrappers(item, attributeHolder))
        {
            yield return powerUpWrapper;
        }
    }

    // TODO: Make this more generic and configurable?
    private IEnumerable<PowerUpWrapper> CreateExcellentAndAncientBasePowerUpWrappers(Item item, AttributeSystem attributeHolder)
    {
        var itemIsExcellent = item.IsExcellent();
        var itemIsAncient = item.IsAncient();

        if (!itemIsAncient && !itemIsExcellent)
        {
            yield break;
        }

        var baseDropLevel = item.Definition!.DropLevel;
        var ancientDropLevel = item.Definition!.CalculateDropLevel(true, false, 0);

        if (InventoryConstants.IsDefenseItemSlot(item.ItemSlot) && !item.IsJewelry())
        {
            var baseDefense = (int)(item.Definition?.BasePowerUpAttributes.FirstOrDefault(a => a.TargetAttribute == Stats.DefenseBase)?.BaseValue ?? 0);
            var additionalDefense = (baseDefense * 12 / baseDropLevel) + (baseDropLevel / 5) + 4;
            yield return new PowerUpWrapper(new SimpleElement(additionalDefense, AggregateType.AddRaw), Stats.DefenseBase, attributeHolder);
            if (itemIsAncient)
            {
                var ancientDefenseBonus = 2 + ((baseDefense + additionalDefense) * 3 / ancientDropLevel) + (ancientDropLevel / 30);
                yield return new PowerUpWrapper(new SimpleElement(ancientDefenseBonus, AggregateType.AddRaw), Stats.DefenseBase, attributeHolder);
            }
        }

        if (item.IsShield())
        {
            var baseDefenseRate = (int)(item.Definition?.BasePowerUpAttributes.FirstOrDefault(a => a.TargetAttribute == Stats.DefenseRatePvm)?.BaseValue ?? 0);
            var additionalRate = (baseDefenseRate * 25 / baseDropLevel) + 5;
            yield return new PowerUpWrapper(new SimpleElement(additionalRate, AggregateType.AddRaw), Stats.DefenseRatePvm, attributeHolder);
            if (itemIsAncient)
            {
                var baseDefense = (int)(item.Definition?.BasePowerUpAttributes.FirstOrDefault(a => a.TargetAttribute == Stats.DefenseBase)?.BaseValue ?? 0);
                var ancientDefenseBonus = 2 + ((baseDefense + item.Level) * 20 / ancientDropLevel);
                yield return new PowerUpWrapper(new SimpleElement(ancientDefenseBonus, AggregateType.AddRaw), Stats.DefenseBase, attributeHolder);
            }
        }

        if (item.IsPhysicalWeapon(out var minPhysDmg))
        {
            var minDmgAttribute = Stats.MinimumPhysBaseDmgByWeapon;
            var maxDmgAttribute = Stats.MaximumPhysBaseDmgByWeapon;
            if (item.ItemSlot == InventoryConstants.RightHandSlot
                && item.Definition!.BasePowerUpAttributes.Any(pu => pu.TargetAttribute == Stats.DoubleWieldWeaponCount || pu.TargetAttribute == Stats.IsOneHandedStaffEquipped))
            {
                minDmgAttribute = Stats.MinPhysBaseDmgByRightWeapon;
                maxDmgAttribute = Stats.MaxPhysBaseDmgByRightWeapon;
            }

            var dmgDivisor = item.Definition!.Group == 5 ? 2 : 1;
            minPhysDmg *= dmgDivisor; // For staffs/sticks the damage is halved initially, so we restore it first

            var additionalDmg = (((int)minPhysDmg * 25 / baseDropLevel) + 5) / dmgDivisor;
            yield return new PowerUpWrapper(new SimpleElement(additionalDmg, AggregateType.AddRaw), minDmgAttribute, attributeHolder);
            yield return new PowerUpWrapper(new SimpleElement(additionalDmg, AggregateType.AddRaw), maxDmgAttribute, attributeHolder);
            if (itemIsAncient)
            {
                var ancientBonus = (5 + (ancientDropLevel / 40)) / dmgDivisor;
                yield return new PowerUpWrapper(new SimpleElement(ancientBonus, AggregateType.AddRaw), minDmgAttribute, attributeHolder);
                yield return new PowerUpWrapper(new SimpleElement(ancientBonus, AggregateType.AddRaw), maxDmgAttribute, attributeHolder);
            }
        }

        if (item.IsWizardryWeapon(out var staffRise) && item.ItemSlot == InventoryConstants.LeftHandSlot)
        {
            var additionalRise = (((int)staffRise * 2 * 25 / baseDropLevel) + 5) / 2;
            yield return new PowerUpWrapper(new SimpleElement(additionalRise, AggregateType.AddRaw), Stats.StaffRise, attributeHolder);
            if (itemIsAncient)
            {
                var ancientRiseBonus = (2 + (ancientDropLevel / 60)) / 2;
                yield return new PowerUpWrapper(new SimpleElement(ancientRiseBonus, AggregateType.AddRaw), Stats.StaffRise, attributeHolder);
            }
        }

        if (item.IsScepter(out var scepterRise))
        {
            var additionalRise = (((int)scepterRise * 2 * 25 / baseDropLevel) + 5) / 2;
            yield return new PowerUpWrapper(new SimpleElement(additionalRise, AggregateType.AddRaw), Stats.ScepterRise, attributeHolder);
            if (itemIsAncient)
            {
                var ancientRiseBonus = (2 + (ancientDropLevel / 60)) / 2;
                yield return new PowerUpWrapper(new SimpleElement(ancientRiseBonus, AggregateType.AddRaw), Stats.ScepterRise, attributeHolder);
            }
        }

        if (item.IsBook(out var curseRise))
        {
            var additionalRise = (((int)curseRise * 2 * 25 / baseDropLevel) + 5) / 2;
            yield return new PowerUpWrapper(new SimpleElement(additionalRise, AggregateType.AddRaw), Stats.BookRise, attributeHolder);
            if (itemIsAncient)
            {
                var ancientRiseBonus = (2 + (ancientDropLevel / 60)) / 2;
                yield return new PowerUpWrapper(new SimpleElement(ancientRiseBonus, AggregateType.AddRaw), Stats.BookRise, attributeHolder);
            }
        }
    }

    private PowerUpWrapper? GetPetLevel(Item item, AttributeSystem attributeHolder)
    {
        const byte darkHorseNumber = 4;

        if (!item.IsTrainablePet())
        {
            return null;
        }

        return new PowerUpWrapper(
            new SimpleElement(item.Level, AggregateType.AddRaw),
            item.Definition?.Number == darkHorseNumber ? Stats.HorseLevel : Stats.RavenLevel,
            attributeHolder);
    }

    /// <summary>
    /// Gets "power downs" which apply to a MG wielding both a staff and a sword (i.e., any one-handed physical weapon).
    /// </summary>
    /// <remarks>
    /// On a staff-sword (mixed) or double staff wield MG, the LH weapon alone dictates the "attack type":
    ///   LH: staff; RH: sword or staff => LH physical damage and rise (ene-MG).
    ///   LH: sword; RH: staff => LH physical damage (str-MG).
    /// Other than these, we are dealing with a str-MG and all weapons' damage attributes count.
    /// </remarks>
    /// <param name="item">The item.</param>
    /// <param name="attributeHolder">The attribute holder.</param>
    /// <param name="skipRightWeaponDmgAttributes">If <c>true</c>, the weapon is being equipped in the <see cref="InventoryConstants.RightHandSlot"/> and its damage attributes should be disregarded.</param>
    /// <returns>A list of rectifying power ups, if any.</returns>
    private IEnumerable<PowerUpWrapper> GetMixedWieldPowerUps(Item item, AttributeSystem attributeHolder, out bool skipRightWeaponDmgAttributes, out bool isRightWieldWeapon)
    {
        List<PowerUpWrapper> powerUps = [];
        skipRightWeaponDmgAttributes = false;
        isRightWieldWeapon = false;

        if (item.ItemSlot == InventoryConstants.RightHandSlot
            && item.Definition!.BasePowerUpAttributes.Any(pu => pu.TargetAttribute == Stats.DoubleWieldWeaponCount || pu.TargetAttribute == Stats.IsOneHandedStaffEquipped))
        {
            isRightWieldWeapon = true;
        }

        /*var isStaff = item.Definition!.BasePowerUpAttributes.Any(pu => pu.TargetAttribute == Stats.IsOneHandedStaffEquipped);
        if (attributeHolder[Stats.IsOneHandedStaffEquipped] > 0 || isStaff)
        {
            if (item.ItemSlot == InventoryConstants.LeftHandSlot && attributeHolder[Stats.DoubleWieldWeaponCount] == 1)
            {
                // Staff-sword wield => nullify RH sword damage
                powerUps.Add(new PowerUpWrapper(new SimpleElement(-1 * attributeHolder[Stats.MinimumPhysBaseDmgByWeapon], AggregateType.AddRaw), Stats.MinimumPhysBaseDmgByWeapon, attributeHolder));
                powerUps.Add(new PowerUpWrapper(new SimpleElement(-1 * attributeHolder[Stats.MaximumPhysBaseDmgByWeapon], AggregateType.AddRaw), Stats.MaximumPhysBaseDmgByWeapon, attributeHolder));
            }
            else if (item.ItemSlot == InventoryConstants.RightHandSlot)
            {
                if (isStaff)
                {
                    // Staff-staff, sword-staff or (none)-staff wield
                    skipRightWeaponDmgAttributes = true;
                }
                else
                {
                    // Staff-sword wield => keep existing damage as final value and nullify raw value
                    powerUps.Add(new PowerUpWrapper(new SimpleElement(attributeHolder[Stats.MinimumPhysBaseDmgByWeapon], AggregateType.AddFinal), Stats.MinimumPhysBaseDmgByWeapon, attributeHolder));
                    powerUps.Add(new PowerUpWrapper(new SimpleElement(attributeHolder[Stats.MaximumPhysBaseDmgByWeapon], AggregateType.AddFinal), Stats.MaximumPhysBaseDmgByWeapon, attributeHolder));
                    powerUps.Add(new PowerUpWrapper(new SimpleElement(0, AggregateType.Multiplicate), Stats.MinimumPhysBaseDmgByWeapon, attributeHolder));
                    powerUps.Add(new PowerUpWrapper(new SimpleElement(0, AggregateType.Multiplicate), Stats.MaximumPhysBaseDmgByWeapon, attributeHolder));
                }
            }
            else
            {
                // Staff-(none) wield => nothing to do here
            }
        }*/

        return powerUps;
    }
}