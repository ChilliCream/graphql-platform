using HotChocolate.Language;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.LogEntryHelper;
using IHasName = HotChocolate.Skimmed.IHasName;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// This composition middleware will apply the @rename directives to subgraphs
/// and rename type system member.
/// </summary>
internal sealed class ApplyRenameDirectiveMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var schema in context.Subgraphs)
        {
            foreach (var directive in  RenameDirective.GetAllFrom(schema, context.FusionTypes))
            {
                if (schema.TryGetMember(directive.Coordinate, out IHasName? member) &&
                    member is IHasContextData memberWithContext)
                {
                    memberWithContext.ContextData[WellKnownContextData.OriginalName] = member.Name;
                }

                if (!schema.RenameMember(directive.Coordinate, directive.NewName))
                {
                    context.Log.Write(RenameMemberNotFound(directive.Coordinate, schema));
                }
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
