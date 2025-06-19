using HotChocolate.Features;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// A type system definition is used in the type initialization to store properties
/// of a type system object.
/// </summary>
public abstract class TypeSystemConfiguration : ITypeSystemConfiguration
{
    private List<TypeDependency>? _dependencies;
    private List<ITypeSystemConfigurationTask>? _configurations;
    private IFeatureCollection? _features;
    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the name of the type system member.
    /// </summary>
    public string Name
    {
        get => _name;
        set => _name = string.Intern(value.EnsureGraphQLName());
    }

    /// <summary>
    /// Gets or sets the description of the type system member.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a name to which this definition is bound to.
    /// </summary>
    public string? BindTo { get; set; }

    /// <summary>
    /// Get access to context data that are copied to the type
    /// and can be used for customizations.
    /// </summary>
    public virtual IFeatureCollection Features
        => _features ??= new FeatureCollection();

    /// <summary>
    /// Gets access to additional type dependencies.
    /// </summary>
    public IList<TypeDependency> Dependencies
        => _dependencies ??= [];

    /// <summary>
    /// Defines if this type has dependencies.
    /// </summary>
    public bool HasDependencies
        => _dependencies is { Count: > 0 };

    /// <summary>
    /// Gets configurations that shall be applied at a later point.
    /// </summary>
    public IList<ITypeSystemConfigurationTask> Tasks
        => _configurations ??= [];

    /// <summary>
    /// Defines if this type has configurations.
    /// </summary>
    public bool HasTasks
        => _configurations is { Count: > 0 };

    /// <summary>
    /// Defines whether descriptor attributes have been applied or not.
    /// </summary>
    public bool AttributesAreApplied { get; set; }

    /// <summary>
    /// Gets lazy configuration of this definition and all dependent definitions.
    /// </summary>
    public virtual IEnumerable<ITypeSystemConfigurationTask> GetTasks()
    {
        if (_configurations is null)
        {
            return [];
        }

        return _configurations;
    }

    /// <summary>
    /// Gets access to additional type dependencies.
    /// </summary>
    public IReadOnlyList<TypeDependency> GetDependencies()
    {
        if (_dependencies is null)
        {
            return [];
        }

        return _dependencies;
    }

    /// <summary>
    /// Get access to features that are copied to the type
    /// and can be used for customizations.
    /// </summary>
    public IFeatureCollection GetFeatures()
        => _features ?? FeatureCollection.Empty;

    /// <summary>
    /// Ensures that a feature collection is created.
    /// </summary>
    public void TouchFeatures()
        => _features = new FeatureCollection();

    protected void CopyTo(TypeSystemConfiguration target)
    {
        if (_dependencies?.Count > 0)
        {
            target._dependencies = [.._dependencies];
        }

        if (_configurations?.Count > 0)
        {
            target._configurations = [];

            foreach (var configuration in _configurations)
            {
                target._configurations.Add(configuration.Copy(target));
            }
        }

        if (_features?.IsEmpty is false)
        {
            target._features = new FeatureCollection();
            foreach (var item in _features)
            {
                target._features[item.Key] = item.Value;
            }
        }

        target.Name = Name;
        target.Description = Description;
        target.AttributesAreApplied = AttributesAreApplied;
        target.BindTo = BindTo;
    }

    protected void MergeInto(TypeSystemConfiguration target)
    {
        if (_dependencies?.Count > 0)
        {
            target._dependencies ??= [];
            target._dependencies.AddRange(_dependencies);
        }

        if (_configurations?.Count > 0)
        {
            target._configurations ??= [];

            foreach (var configuration in _configurations)
            {
                target._configurations.Add(configuration.Copy(target));
            }
        }

        if (_features?.IsEmpty is false)
        {
            target._features ??= new FeatureCollection();
            foreach (var item in _features)
            {
                target._features[item.Key] = item.Value;
            }
        }

        if (target.Description is null && Description is not null)
        {
            target.Description = Description;
        }

        if (BindTo is not null)
        {
            target.BindTo = BindTo;
        }

        if (!target.AttributesAreApplied)
        {
            target.AttributesAreApplied = AttributesAreApplied;
        }
    }

    public override string ToString() => GetType().Name + ": " + Name;
}
