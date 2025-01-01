using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// The <c>@provides</c> directive is used to specify the set of fields on an object type that a
/// resolver provides for the parent type. The <c>fields</c> argument must consist of a valid
/// GraphQL selection set without any directive applications, as directives within the <c>fields</c>
/// argument are not supported.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Provides-Directive-in-Fields-Argument">
/// Specification
/// </seealso>
internal sealed class ProvidesDirectiveInFieldsArgumentRule : IEventHandler<ProvidesFieldNodeEvent>
{
    public void Handle(ProvidesFieldNodeEvent @event, CompositionContext context)
    {
        var (fieldNode, fieldNamePath, providesDirective, field, type, schema) = @event;

        if (fieldNode.Directives.Count != 0)
        {
            context.Log.Write(
                ProvidesDirectiveInFieldsArgument(
                    fieldNamePath,
                    providesDirective,
                    field.Name,
                    type.Name,
                    schema));
        }
    }
}
