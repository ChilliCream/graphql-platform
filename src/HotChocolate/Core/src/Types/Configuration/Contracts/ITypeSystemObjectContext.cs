using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration;

/// <summary>
/// The type system context is available during the type system initialization process.
/// </summary>
public interface ITypeSystemObjectContext : IHasScope, IHasContextData
{
    /// <summary>
    /// The type system object that is being initialized.
    /// </summary>
    ITypeSystemObject Type { get; }

    /// <summary>
    /// A type reference that points to <see cref="Type"/>.
    /// </summary>
    /// <value></value>
    TypeReference TypeReference { get; }

    /// <summary>
    /// Defines if <see cref="Type" /> is a type like the object type or interface type.
    /// </summary>
    bool IsType { get; }

    /// <summary>
    /// Defines if <see cref="Type" /> is an introspection type.
    /// </summary>
    /// <value></value>
    bool IsIntrospectionType { get; }

    /// <summary>
    /// Defines if <see cref="Type" /> is a directive.
    /// </summary>
    bool IsDirective { get; }

    /// <summary>
    /// Defines if <see cref="Type" /> is a schema.
    /// </summary>
    bool IsSchema { get; }

    /// <summary>
    /// Defines if <see cref="Type" /> was inferred from a runtime type.
    /// </summary>
    bool IsInferred { get; }

    /// <summary>
    /// The schema level services.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// The descriptor context that is passed through the initialization process.
    /// </summary>
    IDescriptorContext DescriptorContext { get; }

    /// <summary>
    /// The type initialization interceptor that allows to intercept
    /// objects that er being initialized.
    /// </summary>
    TypeInterceptor TypeInterceptor { get; }

    /// <summary>
    /// Gets the type inspector.
    /// </summary>
    ITypeInspector TypeInspector { get; }

    /// <summary>
    /// Report a schema initialization error.
    /// </summary>
    /// <param name="error">
    /// The error that occurred during initialization.
    /// </param>
    void ReportError(ISchemaError error);

    /// <summary>
    /// Tries to infer the possible type kind from a type reference.
    /// </summary>
    /// <param name="typeRef">
    /// The type reference.
    /// </param>
    /// <param name="kind"></param>
    /// <returns></returns>
    bool TryPredictTypeKind(TypeReference typeRef, out TypeKind kind);
}
