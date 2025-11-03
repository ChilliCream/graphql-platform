using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// A type system definition is used in the type initialization to store properties
/// of a type system object.
/// </summary>
public abstract class TypeSystemConfiguration : ITypeSystemConfiguration
{
    private List<TypeDependency>? _dependencies;
    private List<ITypeSystemConfigurationTask>? _tasks;
    private IFeatureCollection? _features;

    /// <summary>
    /// Gets or sets the name of the type system member.
    /// </summary>
    public virtual string Name
    {
        get;
        set => field = string.Intern(value.EnsureGraphQLName());
    } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the type system member.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a name to which this definition is bound to.
    /// </summary>
    public string? BindTo { get; set; }

    /// <summary>
    /// Defines whether the <see cref="Configurations"/>> have been applied or not.
    /// </summary>
    public bool ConfigurationsAreApplied { get; set; }

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
        => _tasks ??= [];

    /// <summary>
    /// Defines if this type has configurations.
    /// </summary>
    public bool HasTasks
        => _tasks is { Count: > 0 };

    /// <summary>
    /// Gets lazy configuration of this definition and all dependent definitions.
    /// </summary>
    public virtual IEnumerable<ITypeSystemConfigurationTask> GetTasks()
    {
        if (_tasks is null)
        {
            return [];
        }

        return _tasks;
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
        => _features ??= new FeatureCollection();

    protected void CopyTo(TypeSystemConfiguration target)
    {
        if (_dependencies?.Count > 0)
        {
            target._dependencies = [.. _dependencies];
        }

        if (_tasks?.Count > 0)
        {
            target._tasks = [];

            foreach (var configuration in _tasks)
            {
                target._tasks.Add(configuration.Copy(target));
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
        target.ConfigurationsAreApplied = ConfigurationsAreApplied;
        target.BindTo = BindTo;
    }

    protected void MergeInto(TypeSystemConfiguration target)
    {
        if (_dependencies?.Count > 0)
        {
            target._dependencies ??= [];
            target._dependencies.AddRange(_dependencies);
        }

        if (_tasks?.Count > 0)
        {
            target._tasks ??= [];

            foreach (var configuration in _tasks)
            {
                target._tasks.Add(configuration.Copy(target));
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

        if (!target.ConfigurationsAreApplied)
        {
            target.ConfigurationsAreApplied = ConfigurationsAreApplied;
        }
    }

    public override string ToString() => GetType().Name + ": " + Name;
}
