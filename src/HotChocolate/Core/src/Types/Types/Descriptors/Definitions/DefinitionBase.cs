using System.Collections.Immutable;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// A type system definition is used in the type initialization to store properties
/// of a type system object.
/// </summary>
public class DefinitionBase : IDefinition
{
    private List<TypeDependency>? _dependencies;
    private List<ITypeSystemMemberConfiguration>? _configurations;
    private ExtensionData? _contextData;
    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the name the type shall have.
    /// </summary>
    public string Name
    {
        get => _name;
        set => _name = string.Intern(value.EnsureGraphQLName());
    }

    /// <summary>
    /// Gets or sets the description the type shall have.
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
    public virtual ExtensionData ContextData
        => _contextData ??= new ExtensionData();

    /// <summary>
    /// Gets access to additional type dependencies.
    /// </summary>
    public IList<TypeDependency> Dependencies
        => _dependencies ??= [];

    /// <summary>
    /// Defines if this type has dependencies.
    /// </summary>
    public bool HasDependencies
        => _dependencies is { Count: > 0, };

    /// <summary>
    /// Gets configurations that shall be applied at a later point.
    /// </summary>
    public IList<ITypeSystemMemberConfiguration> Configurations
        => _configurations ??= [];

    /// <summary>
    /// Defines if this type has configurations.
    /// </summary>
    public bool HasConfigurations
        => _configurations is { Count: > 0, };

    /// <summary>
    /// Defines whether descriptor attributes have been applied or not.
    /// </summary>
    public bool AttributesAreApplied { get; set; }

    /// <summary>
    /// Gets state that is available during schema initialization.
    /// </summary>
    public ImmutableDictionary<string, object?> State { get; set; }
        = ImmutableDictionary<string, object?>.Empty;

    /// <summary>
    /// Gets lazy configuration of this definition and all dependent definitions.
    /// </summary>
    public virtual IEnumerable<ITypeSystemMemberConfiguration> GetConfigurations()
    {
        if (_configurations is null)
        {
            return Array.Empty<ITypeSystemMemberConfiguration>();
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
            return Array.Empty<TypeDependency>();
        }

        return _dependencies;
    }

    /// <summary>
    /// Get access to context data that are copied to the type
    /// and can be used for customizations.
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetContextData()
    {
        if (_contextData is null)
        {
            return ImmutableDictionary<string, object?>.Empty;
        }

        return _contextData;
    }

    public void TouchContextData()
        => _contextData = new ExtensionData();

    protected void CopyTo(DefinitionBase target)
    {
        if (_dependencies is not null && _dependencies.Count > 0)
        {
            target._dependencies = [.._dependencies,];
        }

        if (_configurations is not null && _configurations.Count > 0)
        {
            target._configurations = [];

            foreach (var configuration in _configurations)
            {
                target._configurations.Add(configuration.Copy(target));
            }
        }

        if (_contextData is not null && _contextData.Count > 0)
        {
            target._contextData = new ExtensionData(_contextData);
        }

        if (State is { Count: > 0 })
        {
            target.State = State;
        }

        target.Name = Name;
        target.Description = Description;
        target.AttributesAreApplied = AttributesAreApplied;
        target.BindTo = BindTo;
    }

    protected void MergeInto(DefinitionBase target)
    {
        if (_dependencies is not null && _dependencies.Count > 0)
        {
            target._dependencies ??= [];
            target._dependencies.AddRange(_dependencies);
        }

        if (_configurations is not null && _configurations.Count > 0)
        {
            target._configurations ??= [];

            foreach (var configuration in _configurations)
            {
                target._configurations.Add(configuration.Copy(target));
            }
        }

        if (_contextData is not null && _contextData.Count > 0)
        {
            target._contextData ??= new ExtensionData();
            foreach (var item in _contextData)
            {
                target._contextData[item.Key] = item.Value;
            }
        }

        if (State is { Count: > 0 })
        {
            if (target.State.Count == 0)
            {
                target.State = State;
            }
            else
            {
                var state = ImmutableDictionary.CreateBuilder<string, object?>();
                if (target.State.Count > 0)
                {
                    state.AddRange(target.State);
                }

                state.AddRange(State);
                target.State = state.ToImmutable();
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
