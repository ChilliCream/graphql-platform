using System.Diagnostics;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types;

public abstract class TypeSystemObjectBase : ITypeSystemObject
{
    private TypeStatus _status;
    private string? _name;
    private string? _scope;
    private string? _description;

    /// <summary>
    /// Gets a scope name that was provided by an extension.
    /// </summary>
    public string? Scope
    {
        get => _scope;
        protected set
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException(
                    "The type scope is immutable.");
            }
            _scope = value;
        }
    }

    /// <summary>
    /// Gets the GraphQL type name.
    /// </summary>
    public string Name
    {
        get => _name!;
        protected set
        {
            if (IsNamed)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeSystemObject_NameImmutable);
            }
            _name = value.EnsureGraphQLName();
        }
    }

    /// <summary>
    /// Gets the optional description of this scalar type.
    /// </summary>
    public string? Description
    {
        get => _description;
        protected set
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeSystemObject_DescriptionImmutable);
            }
            _description = value;
        }
    }

    public abstract IReadOnlyDictionary<string, object?> ContextData { get; }

    protected internal bool IsInitialized
        => _status is TypeStatus.Initialized or TypeStatus.Named or TypeStatus.Completed;

    protected internal bool IsNamed
        => _status is TypeStatus.Named or TypeStatus.Completed;

    protected internal bool IsCompleted
        => _status is TypeStatus.Completed;

    /// <summary>
    /// The type configuration is created and dependencies are registered.
    /// </summary>
    internal virtual void Initialize(ITypeDiscoveryContext context)
    {
        MarkInitialized();
    }

    /// <summary>
    /// If this type has a dynamic type it will be completed in this step.
    /// </summary>
    internal virtual void CompleteName(ITypeCompletionContext context)
    {
        MarkNamed();
    }

    /// <summary>
    /// All type properties are set and the type settings are completed.
    /// </summary>
    internal virtual void CompleteType(ITypeCompletionContext context)
    {
        MarkCompleted();
    }

    /// <summary>
    /// All types are completed at this point and the type can clean up any
    /// temporary data structures.
    ///
    /// This step is mainly to cleanup.
    /// </summary>
    internal virtual void FinalizeType(ITypeCompletionContext context)
    {
        MarkFinalized();
    }

    protected void MarkInitialized()
    {
        Debug.Assert(_status == TypeStatus.Uninitialized);

        if (_status != TypeStatus.Uninitialized)
        {
            throw new InvalidOperationException();
        }

        _status = TypeStatus.Initialized;
    }

    protected void MarkNamed()
    {
        Debug.Assert(_status == TypeStatus.Initialized);

        if (_status != TypeStatus.Initialized)
        {
            throw new InvalidOperationException();
        }

        _status = TypeStatus.Named;
    }

    protected void MarkCompleted()
    {
        Debug.Assert(_status == TypeStatus.Named);

        if (_status != TypeStatus.Named)
        {
            throw new InvalidOperationException();
        }

        _status = TypeStatus.Completed;
    }

    protected void MarkFinalized()
    {
        Debug.Assert(_status == TypeStatus.Completed);

        if (_status != TypeStatus.Completed)
        {
            throw new InvalidOperationException();
        }

        _status = TypeStatus.Finalized;
    }
}
