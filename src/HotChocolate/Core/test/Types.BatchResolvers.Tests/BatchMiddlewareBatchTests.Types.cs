using System.Collections.Immutable;
using System.Reflection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class BatchMiddlewareBatchTests
{
    private static void Common(IRequestExecutorBuilder builder)
        => builder.AddDirectiveType(new DirectiveType(d =>
        {
            d.Name("trace").Location(DirectiveLocation.FieldDefinition).Repeatable();
            d.Argument("marker").Type<NonNullType<StringType>>();
            d.UseBatch(CreateDirectiveMiddleware);
        }));

    private static BatchFieldDelegate CreateDirectiveMiddleware(BatchFieldDelegate next, Directive directive)
        => contexts =>
        {
            var trace = contexts[0].Services.GetRequiredService<MiddlewareTrace>();
            return TraceBatchMiddleware.Wrap(next, GetMarker(directive), trace)(contexts);
        };

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder.AddQueryType<MiddlewareAttributeQuery>();
    }

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder.AddQueryType(MiddlewareQuery.Initialize);
    }

    private void ConfigureFluent(IRequestExecutorBuilder builder)
    {
        Common(builder);

        builder.AddQueryType(d =>
        {
            d.Name("Query");
            d.Field("tracedById")
                .Argument("id", a => a.Type<NonNullType<IntType>>())
                .UseBatch<ClassTraceMiddleware>()
                .UseBatch((sp, next) => new FactoryTraceMiddleware(next, sp.GetRequiredService<MiddlewareTrace>()))
                .ResolveBatchWith<FluentMiddlewareResolvers>(t => t.GetTracedById(null!));
            d.Field("directiveTracedById")
                .Argument("id", a => a.Type<NonNullType<IntType>>())
                .Directive("trace", new ArgumentNode("marker", "A"))
                .Directive("trace", new ArgumentNode("marker", "B"))
                .ResolveBatchWith<FluentMiddlewareResolvers>(t => t.GetDirectiveTracedById(null!));
        });
    }

    private static string GetMarker(Directive directive)
        => directive.ToValue<Dictionary<string, object?>>()["marker"]!.ToString()!;
}

/// <summary>
/// Records the order in which stacked batch middleware layers enter and exit.
/// </summary>
public sealed class MiddlewareTrace
{
    public List<string> Events { get; } = [];
}

internal static class TraceBatchMiddleware
{
    public static BatchFieldDelegate Wrap(BatchFieldDelegate next, string marker, MiddlewareTrace trace)
        => async contexts =>
        {
            trace.Events.Add($"{marker}:before");
            await next(contexts);
            foreach (var context in contexts)
            {
                context.Result = $"{marker}({context.Result})";
            }

            trace.Events.Add($"{marker}:after");
        };
}

/// <summary>
/// Class-based batch middleware activated through <c>descriptor.UseBatch&lt;T&gt;()</c>, either
/// directly (fluent) or from within an <see cref="ObjectFieldDescriptorAttribute"/> forwarded by
/// the source generator's <c>ConfigurationHelper.ApplyConfiguration</c> (attribute-based and
/// source-generated styles).
/// </summary>
public sealed class ClassTraceMiddleware(BatchFieldDelegate next, MiddlewareTrace trace)
{
    public ValueTask InvokeAsync(ImmutableArray<IMiddlewareContext> contexts)
        => TraceBatchMiddleware.Wrap(next, "class", trace)(contexts);
}

/// <summary>
/// Factory-based batch middleware activated through <c>descriptor.UseBatch(factory)</c>.
/// </summary>
public sealed class FactoryTraceMiddleware(BatchFieldDelegate next, MiddlewareTrace trace)
{
    public ValueTask InvokeAsync(ImmutableArray<IMiddlewareContext> contexts)
        => TraceBatchMiddleware.Wrap(next, "factory", trace)(contexts);
}

/// <summary>
/// Applies <see cref="ClassTraceMiddleware"/> through the descriptor-attribute forwarding path
/// so attribute-based and source-generated resolvers exercise the same class-based registration
/// as the fluent style.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class UseClassTraceAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
        => descriptor.UseBatch<ClassTraceMiddleware>();
}

/// <summary>
/// Applies <see cref="FactoryTraceMiddleware"/> through the descriptor-attribute forwarding path
/// so attribute-based and source-generated resolvers exercise the same factory-based
/// registration as the fluent style.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class UseFactoryTraceAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
        => descriptor.UseBatch(
            (sp, next) => new FactoryTraceMiddleware(next, sp.GetRequiredService<MiddlewareTrace>()));
}

/// <summary>
/// Applies the repeatable <c>trace</c> directive through the descriptor-attribute forwarding
/// path, proving directive-based batch middleware activates and composes in registration order
/// for attribute-based and source-generated resolvers, not only for hand-written fluent code.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class TraceDirectiveAttribute(string marker) : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
        => descriptor.Directive("trace", new ArgumentNode("marker", marker));
}

/// <summary>
/// Fluent-style batch resolvers for both middleware rows.
/// </summary>
public sealed class FluentMiddlewareResolvers
{
    public List<string> GetTracedById(List<int> id) => id.ConvertAll(_ => "value");

    public List<string> GetDirectiveTracedById(List<int> id) => id.ConvertAll(_ => "value");
}

/// <summary>
/// Attribute-style root query applying class/factory middleware and repeated directive
/// middleware to <c>[BatchResolver]</c> fields.
/// </summary>
public sealed class MiddlewareAttributeQuery
{
    [BatchResolver]
    [UseClassTrace]
    [UseFactoryTrace]
    public List<string> GetTracedById(List<int> id) => id.ConvertAll(_ => "value");

    [BatchResolver]
    [TraceDirective("A")]
    [TraceDirective("B")]
    public List<string> GetDirectiveTracedById(List<int> id) => id.ConvertAll(_ => "value");
}

/// <summary>
/// Source-generated root query applying the same descriptor attributes as
/// <see cref="MiddlewareAttributeQuery"/>, forwarded through the generator's
/// <c>ConfigurationHelper.ApplyConfiguration</c> (hc-0-bpl.1 comment 160).
/// </summary>
[QueryType]
public static partial class MiddlewareQuery
{
    [BatchResolver]
    [UseClassTrace]
    [UseFactoryTrace]
    public static List<string> GetTracedById(List<int> id) => id.ConvertAll(_ => "value");

    [BatchResolver]
    [TraceDirective("A")]
    [TraceDirective("B")]
    public static List<string> GetDirectiveTracedById(List<int> id) => id.ConvertAll(_ => "value");
}
