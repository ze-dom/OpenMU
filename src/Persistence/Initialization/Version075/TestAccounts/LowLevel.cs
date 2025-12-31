// <copyright file="LowLevel.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Version075.TestAccounts;

using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;

/// <summary>
/// Initializer for an account with low level characters.
/// </summary>
internal class LowLevel : AccountInitializerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LowLevel"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="accountName">Name of the account.</param>
    /// <param name="level">The level.</param>
    public LowLevel(IContext context, GameConfiguration gameConfiguration, string accountName, int level)
        : base(context, gameConfiguration, accountName, level)
    {
    }

    /// <inheritdoc/>
    protected override Character CreateKnight()
    {
        var character = this.CreateCharacter(this.AccountName + "Dk", CharacterClassNumber.DarkKnight, this.Level, 0);
        character.Inventory!.Items.Add(this.CreateSmallAxe(0));
        character.Inventory.Items.Add(this.CreateArmorItem(52, 5, 8)); // Leather Armor
        character.Inventory.Items.Add(this.CreateArmorItem(47, 5, 7)); // Leather Helm
        character.Inventory.Items.Add(this.CreateArmorItem(49, 5, 9)); // Leather Pants
        character.Inventory.Items.Add(this.CreateArmorItem(63, 5, 10)); // Leather Gloves
        character.Inventory.Items.Add(this.CreateArmorItem(65, 5, 11)); // Leather Boots
        this.AddTestJewelsAndPotions(character.Inventory);
        return character;
    }

    /// <inheritdoc/>
    protected override Character CreateElf()
    {
        var character = this.CreateCharacter(this.AccountName + "Elf", CharacterClassNumber.FairyElf, this.Level, 2);
        character.Attributes.First(a => a.Definition == Stats.BaseStrength).Value += 20;
        character.Attributes.First(a => a.Definition == Stats.BaseAgility).Value += 30;

        character.Inventory!.Items.Add(this.CreateShortBow(1));
        character.Inventory.Items.Add(this.CreateArrows(0));
        character.Inventory.Items.Add(this.CreateArmorItem(InventoryConstants.ArmorSlot, 10, 8, 9, 1, true)); // Vine Armor
        character.Inventory.Items.Add(this.CreateArmorItem(InventoryConstants.HelmSlot, 10, 7, 10, 2, true)); // Vine Helm
        character.Inventory.Items.Add(this.CreateArmorItem(InventoryConstants.PantsSlot, 10, 9, 11, 3, true)); // Vine Pants
        character.Inventory.Items.Add(this.CreateArmorItem(InventoryConstants.GlovesSlot, 10, 10, 10, 4)); // Vine Gloves
        character.Inventory.Items.Add(this.CreateArmorItem(InventoryConstants.BootsSlot, 10, 11, 9, 4)); // Vine Boots

        character.Inventory.Items.Add(this.CreateOrb(67, 8)); // Healing Orb
        character.Inventory.Items.Add(this.CreateOrb(75, 9)); // Defense Orb
        character.Inventory.Items.Add(this.CreateOrb(68, 10)); // Damage Orb

        var wings = this.Context.CreateNew<Item>();
        wings.Definition = this.GameConfiguration.Items.First(def => def.Group == 12 && def.Number == 0);
        wings.Durability = wings.Definition?.Durability ?? 0;
        wings.ItemSlot = 36;
        character.Inventory.Items.Add(wings);

        for (int i = 0; i < 3; i++)
        {
            var uniria = this.Context.CreateNew<Item>();
            uniria.Definition = this.GameConfiguration.Items.First(def => def.Group == 13 && def.Number == 2);
            uniria.Durability = uniria.Definition?.Durability ?? 0;
            uniria.ItemSlot = (byte)(53 + i);
            character.Inventory.Items.Add(uniria);
        }

        for (int i = 0; i < 5; i++)
        {
            var jol = this.Context.CreateNew<Item>();
            jol.Definition = this.GameConfiguration.Items.First(def => def.Group == 14 && def.Number == 16);
            jol.Durability = jol.Definition?.Durability ?? 0;
            jol.ItemSlot = (byte)(56 + i);
            character.Inventory.Items.Add(jol);
        }

        for (int i = 0; i < 4; i++)
        {
            var deye = this.Context.CreateNew<Item>();
            deye.Definition = this.GameConfiguration.Items.First(def => def.Group == 14 && def.Number == 17);
            deye.Level = (byte)(i + 1);
            deye.ItemSlot = (byte)(60 + i);
            character.Inventory.Items.Add(deye);
        }

        for (int i = 0; i < 4; i++)
        {
            var dkey = this.Context.CreateNew<Item>();
            dkey.Definition = this.GameConfiguration.Items.First(def => def.Group == 14 && def.Number == 18);
            dkey.Level = (byte)(i + 1);
            dkey.ItemSlot = (byte)(64 + i);
            character.Inventory.Items.Add(dkey);
        }

        this.AddElfItems(character.Inventory);
        return character;
    }

    /// <inheritdoc/>
    protected override Character CreateWizard()
    {
        var character = this.CreateCharacter(this.AccountName + "Dw", CharacterClassNumber.DarkWizard, this.Level, 1);
        character.Inventory!.Items.Add(this.CreateSkullStaff(0));
        character.Inventory.Items.Add(this.CreateArmorItem(52, 2, 8)); // Pad Armor
        character.Inventory.Items.Add(this.CreateArmorItem(47, 2, 7)); // Pad Helm
        character.Inventory.Items.Add(this.CreateArmorItem(49, 2, 9)); // Pad Pants
        character.Inventory.Items.Add(this.CreateArmorItem(63, 2, 10)); // Pad Gloves
        character.Inventory.Items.Add(this.CreateArmorItem(65, 2, 11)); // Pad Boots
        this.AddTestJewelsAndPotions(character.Inventory);
        this.AddScrolls(character.Inventory);
        return character;
    }

    private Item CreateSkullStaff(byte itemSlot)
    {
        var skullStaff = this.Context.CreateNew<Item>();
        skullStaff.Definition = this.GameConfiguration.Items.First(def => def.Group == 5 && def.Number == 0); // skull staff
        skullStaff.Durability = skullStaff.Definition?.Durability ?? 0;
        skullStaff.ItemSlot = itemSlot;
        return skullStaff;
    }

    private Item CreateSmallAxe(byte itemSlot)
    {
        var smallAxe = this.Context.CreateNew<Item>();
        smallAxe.Definition = this.GameConfiguration.Items.First(def => def.Group == 1 && def.Number == 0); // small axe
        smallAxe.Durability = smallAxe.Definition?.Durability ?? 0;
        smallAxe.ItemSlot = itemSlot;
        return smallAxe;
    }

    private Item CreateShortBow(byte itemSlot)
    {
        var shortBow = this.Context.CreateNew<Item>();
        shortBow.Definition = this.GameConfiguration.Items.First(def => def.Group == 4 && def.Number == 0); // short bow
        shortBow.Durability = shortBow.Definition?.Durability ?? 0;
        shortBow.ItemSlot = itemSlot;
        return shortBow;
    }
}