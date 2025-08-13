using System.Diagnostics;
using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

/// <summary>
/// The base class for all GraphQL type system objects.
/// </summary>
public abstract class TypeSystemObject : ITypeSystemMember, IFeatureProvider
{
    private TypeStatus _status;
    private string? _name;
    private string? _scope;
    private string? _description;

    /// <summary>
    /// Gets a scope name provided by an extension.
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

    public abstract IFeatureCollection Features { get; }

    protected internal bool IsInitialized
        => _status is TypeStatus.Initialized
            or TypeStatus.Named
            or TypeStatus.Completed
            or TypeStatus.MetadataCompleted
            or TypeStatus.Executable
            or TypeStatus.Finalized;

    protected internal bool IsNamed
        => _status is TypeStatus.Named
            or TypeStatus.Completed
            or TypeStatus.MetadataCompleted
            or TypeStatus.Executable
            or TypeStatus.Finalized;

    protected internal bool IsCompleted
        => _status is TypeStatus.Completed
            or TypeStatus.MetadataCompleted
            or TypeStatus.Executable
            or TypeStatus.Finalized;

    protected internal bool IsMetadataCompleted
        => _status is TypeStatus.MetadataCompleted
            or TypeStatus.Executable
            or TypeStatus.Finalized;

    protected internal bool IsExecutable
        => _status is TypeStatus.Executable
            or TypeStatus.Finalized;

    protected internal bool IsSealed
        => _status is TypeStatus.Finalized;

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
    /// All type properties are set and the type structure is completed.
    /// </summary>
    internal virtual void CompleteType(ITypeCompletionContext context)
    {
        MarkCompleted();
    }

    /// <summary>
    /// All type system directives are completed.
    /// </summary>
    internal virtual void CompleteMetadata(ITypeCompletionContext context)
    {
        MarkMetadataCompleted();
    }

    /// <summary>
    /// All resolvers are compiled, and the schema becomes executable.
    /// </summary>
    internal virtual void MakeExecutable(ITypeCompletionContext context)
    {
        MarkExecutable();
    }

    /// <summary>
    /// <para>
    /// All types are completed at this point and the type can clean up any
    /// temporary data structures.
    /// </para>
    /// <para>
    /// This step is mainly to cleanup.
    /// </para>
    /// </summary>
    internal virtual void FinalizeType(ITypeCompletionContext context)
    {
        MarkFinalized();
    }

    private protected void MarkInitialized()
    {
        Debug.Assert(_status == TypeStatus.Uninitialized);

        if (_status != TypeStatus.Uninitialized)
        {
            throw new InvalidOperationException();
        }

        _status = TypeStatus.Initialized;
    }

    private protected void MarkNamed()
    {
        Debug.Assert(_status == TypeStatus.Initialized);

        if (_status != TypeStatus.Initialized)
        {
            throw new InvalidOperationException();
        }

        _status = TypeStatus.Named;
    }

    private protected void MarkCompleted()
    {
        Debug.Assert(_status == TypeStatus.Named);

        if (_status != TypeStatus.Named)
        {
            throw new InvalidOperationException();
        }

        _status = TypeStatus.Completed;
    }

    private protected void MarkMetadataCompleted()
    {
        Debug.Assert(_status == TypeStatus.Completed);

        if (_status != TypeStatus.Completed)
        {
            throw new InvalidOperationException();
        }

        _status = TypeStatus.MetadataCompleted;
    }

    private protected void MarkExecutable()
    {
        Debug.Assert(_status == TypeStatus.MetadataCompleted);

        if (_status != TypeStatus.MetadataCompleted)
        {
            throw new InvalidOperationException();
        }

        _status = TypeStatus.Executable;
    }

    private protected void MarkFinalized()
    {
        Debug.Assert(_status == TypeStatus.Executable);

        if (_status != TypeStatus.Executable)
        {
            throw new InvalidOperationException();
        }

        _status = TypeStatus.Finalized;
    }
}
