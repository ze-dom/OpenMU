// <copyright file="ItemSetGroup.Generated.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

//------------------------------------------------------------------------------
// <auto-generated>
//     This source code was auto-generated by a roslyn code generator.
// </auto-generated>
//------------------------------------------------------------------------------

// ReSharper disable All

namespace MUnique.OpenMU.Persistence.BasicModel;

using MUnique.OpenMU.Persistence.Json;

/// <summary>
/// A plain implementation of <see cref="ItemSetGroup"/>.
/// </summary>
public partial class ItemSetGroup : MUnique.OpenMU.DataModel.Configuration.Items.ItemSetGroup, IIdentifiable, IConvertibleTo<ItemSetGroup>
{
    
    /// <summary>
    /// Gets or sets the identifier of this instance.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets the raw collection of <see cref="Options" />.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("options")]
    public ICollection<IncreasableItemOption> RawOptions { get; } = new List<IncreasableItemOption>();
    
    /// <inheritdoc/>
    [System.Text.Json.Serialization.JsonIgnore]
    public override ICollection<MUnique.OpenMU.DataModel.Configuration.Items.IncreasableItemOption> Options
    {
        get => base.Options ??= new CollectionAdapter<MUnique.OpenMU.DataModel.Configuration.Items.IncreasableItemOption, IncreasableItemOption>(this.RawOptions);
        protected set
        {
            this.Options.Clear();
            foreach (var item in value)
            {
                this.Options.Add(item);
            }
        }
    }

    /// <summary>
    /// Gets the raw collection of <see cref="Items" />.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("items")]
    public ICollection<ItemOfItemSet> RawItems { get; } = new List<ItemOfItemSet>();
    
    /// <inheritdoc/>
    [System.Text.Json.Serialization.JsonIgnore]
    public override ICollection<MUnique.OpenMU.DataModel.Configuration.Items.ItemOfItemSet> Items
    {
        get => base.Items ??= new CollectionAdapter<MUnique.OpenMU.DataModel.Configuration.Items.ItemOfItemSet, ItemOfItemSet>(this.RawItems);
        protected set
        {
            this.Items.Clear();
            foreach (var item in value)
            {
                this.Items.Add(item);
            }
        }
    }


    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        var baseObject = obj as IIdentifiable;
        if (baseObject != null)
        {
            return baseObject.Id == this.Id;
        }

        return base.Equals(obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this.Id.GetHashCode();
    }

    /// <inheritdoc/>
    public ItemSetGroup Convert() => this;
}
