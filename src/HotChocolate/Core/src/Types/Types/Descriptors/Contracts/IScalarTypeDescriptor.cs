using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// A fluent configuration API for GraphQL scalar types.
/// </summary>
public interface IScalarTypeDescriptor
    : IDescriptor<ScalarTypeDefinition>
    , IFluent
{
    /// <summary>
    /// Sets a directive on the object type
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive(new MyDirective());
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// scalar Foo @myDirective
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="directiveInstance">
    /// The instance of the directive
    /// </param>
    /// <typeparam name="TDirective">The type of the directive</typeparam>
    IScalarTypeDescriptor Directive<TDirective>(
        TDirective directiveInstance)
        where TDirective : class;

    /// <summary>
    /// Sets a directive on the object type
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive&lt;MyDirective>();
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// scalar Foo @myDirective
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="TDirective">The type of the directive</typeparam>
    IScalarTypeDescriptor Directive<TDirective>()
        where TDirective : class, new();

    /// <summary>
    /// Sets a directive on the object type
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive("myDirective");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// scalar Foo @myDirective
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="name">The name of the directive</param>
    /// <param name="arguments">The arguments of the directive</param>
    IScalarTypeDescriptor Directive(
        string name,
        params ArgumentNode[] arguments);
}
