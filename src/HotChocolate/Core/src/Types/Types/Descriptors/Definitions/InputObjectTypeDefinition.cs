using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Defines the properties of a GraphQL input object type.
/// </summary>
public class InputObjectTypeDefinition : TypeDefinitionBase<InputObjectTypeDefinitionNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeDefinition"/>.
    /// </summary>
    public InputObjectTypeDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeDefinition"/>.
    /// </summary>
    public InputObjectTypeDefinition(
        NameString name,
        string? description = null,
        Type? runtimeType = null)
        : base(runtimeType ?? typeof(object))
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Gets the input fields.
    /// </summary>
    public IBindableList<InputFieldDefinition> Fields { get; } =
        new BindableList<InputFieldDefinition>();

    /// <summary>
    /// Gets or sets the input object runtime value factory delegate.
    /// </summary>
    public Func<object?[], object>? CreateInstance { get; set; }

    /// <summary>
    /// Gets or sets the delegate to extract the field values from the runtime value.
    /// </summary>
    public Action<object, object?[]>? GetFieldData { get; set; }

    public override IEnumerable<ITypeSystemMemberConfiguration> GetConfigurations()
    {
        List<ITypeSystemMemberConfiguration>? configs = null;

        if (HasConfigurations)
        {
            configs ??= new();
            configs.AddRange(Configurations);
        }

        foreach (InputFieldDefinition field in Fields)
        {
            if (field.HasConfigurations)
            {
                configs ??= new();
                configs.AddRange(field.Configurations);
            }
        }

        return configs ?? Enumerable.Empty<ITypeSystemMemberConfiguration>();
    }

    protected internal void CopyTo(InputObjectTypeDefinition target)
    {
        base.CopyTo(target);

        if (Fields is { Count: > 0 })
        {
            target.Fields.Clear();

            foreach (InputFieldDefinition? field in Fields)
            {
                target.Fields.Add(field);
            }
        }

        target.CreateInstance = CreateInstance;
        target.GetFieldData = GetFieldData;
    }

    protected internal void MergeInto(InputObjectTypeDefinition target)
    {
        base.MergeInto(target);

        foreach (InputFieldDefinition? field in Fields)
        {
            InputFieldDefinition? targetField =
                target.Fields.FirstOrDefault(t => field.Name.Equals(t.Name));

            if (field.Ignore)
            {
                if (targetField is not null)
                {
                    target.Fields.Remove(targetField);
                }
            }
            else if (targetField is null)
            {
                if (targetField is not null)
                {
                    target.Fields.Remove(targetField);
                }

                var newField = new InputFieldDefinition();
                field.CopyTo(newField);
                target.Fields.Add(newField);
            }
            else
            {
                field.MergeInto(targetField);
            }
        }

        target.CreateInstance ??= CreateInstance;
        target.GetFieldData ??= GetFieldData;
    }
}

