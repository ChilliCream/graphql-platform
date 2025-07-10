#pragma warning disable IDE1006 // Naming Styles
#nullable enable

using System.Text;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Types.Introspection;

// ReSharper disable once InconsistentNaming
internal static class __Schema
{
    private static readonly Encoding _utf8 = Encoding.UTF8;

    public static ValueTask Description(FieldContext context, CancellationToken cancellationToken = default)
    {
        if (context.Schema.Description is not null)
        {
            var expectedSize = _utf8.GetByteCount(context.Schema.Description);
            var span = context.Memory.GetSpan(expectedSize);
            var written = _utf8.GetBytes(context.Schema.Description, span);
            context.Memory.Advance(written);
        }

        return ValueTask.CompletedTask;
    }

    public static ValueTask Types(FieldContext context, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public static ValueTask QueryType(FieldContext context, CancellationToken cancellationToken = default)
    {

    }

    public static object? MutationType(FieldContext context, CancellationToken cancellationToken = default)
        => context.Parent<ISchemaDefinition>().MutationType;

    public static object? SubscriptionType(FieldContext context, CancellationToken cancellationToken = default)
        => context.Parent<ISchemaDefinition>().SubscriptionType;

    public static object Directives(FieldContext context, CancellationToken cancellationToken = default)
        => context.Parent<ISchemaDefinition>()
            .DirectiveDefinitions
            .Where(t => Unsafe.As<DirectiveType>(t).IsPublic);

    public static object AppliedDirectives(FieldContext context, CancellationToken cancellationToken = default)
        => context.Parent<ISchemaDefinition>().Directives
            .Where(t => Unsafe.As<DirectiveType>(t).IsPublic)
            .Select(d => d.ToSyntaxNode());

}

// ReSharper disable once InconsistentNaming
internal static class __Type
{
    
}
#pragma warning restore IDE1006 // Naming Styles
