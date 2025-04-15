using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Defines the properties of a GraphQL input object type.
/// </summary>
public class InputObjectTypeConfiguration : TypeDefinitionBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="InputObjectTypeConfiguration"/>.
    /// </summary>
    public InputObjectTypeConfiguration() { }

    /// <summary>
    /// Initializes a new instance of <see cref="InputObjectTypeConfiguration"/>.
    /// </summary>
    public InputObjectTypeConfiguration(
        string name,
        string? description = null,
        Type? runtimeType = null)
        : base(runtimeType ?? typeof(object))
    {
        Name = name.EnsureGraphQLName();
        Description = description;
    }

    /// <summary>
    /// Gets the input fields.
    /// </summary>
    public IBindableList<InputFieldConfiguration> Fields { get; } =
        new BindableList<InputFieldConfiguration>();

    /// <summary>
    /// Gets or sets the input object runtime value factory delegate.
    /// </summary>
    public Func<object?[], object>? CreateInstance { get; set; }

    /// <summary>
    /// Gets or sets the delegate to extract the field values from the runtime value.
    /// </summary>
    public Action<object, object?[]>? GetFieldData { get; set; }

    public override IEnumerable<ITypeSystemConfigurationTask> GetConfigurations()
    {
        List<ITypeSystemConfigurationTask>? configs = null;

        if (HasConfigurations)
        {
            configs ??= [];
            configs.AddRange(Configurations);
        }

        foreach (var field in Fields)
        {
            if (field.HasConfigurations)
            {
                configs ??= [];
                configs.AddRange(field.Configurations);
            }
        }

        return configs ?? Enumerable.Empty<ITypeSystemConfigurationTask>();
    }

    protected internal void CopyTo(InputObjectTypeConfiguration target)
    {
        base.CopyTo(target);

        if (Fields is { Count: > 0, })
        {
            target.Fields.Clear();

            foreach (var field in Fields)
            {
                target.Fields.Add(field);
            }
        }

        target.CreateInstance = CreateInstance;
        target.GetFieldData = GetFieldData;
    }

    protected internal void MergeInto(InputObjectTypeConfiguration target)
    {
        base.MergeInto(target);

        foreach (var field in Fields)
        {
            var targetField =
                target.Fields.FirstOrDefault(t => field.Name.EqualsOrdinal(t.Name));

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

                var newField = new InputFieldConfiguration();
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
