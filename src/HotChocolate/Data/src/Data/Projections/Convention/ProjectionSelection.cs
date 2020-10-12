using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections
{
    public class ProjectionSelection
        : Selection
        , IProjectionSelection
    {
        public ProjectionSelection(
            IProjectionFieldHandler handler,
            IObjectType declaringType,
            IObjectField field,
            FieldNode selection,
            FieldDelegate resolverPipeline,
            NameString? responseName = null,
            IReadOnlyDictionary<NameString, ArgumentValue>? arguments = null,
            SelectionIncludeCondition? includeCondition = null,
            bool internalSelection = false) : base(
            declaringType,
            field,
            selection,
            resolverPipeline,
            responseName,
            arguments,
            includeCondition,
            internalSelection)
        {
            Handler = handler;
        }

        public IProjectionFieldHandler Handler { get; }

        public static ProjectionSelection From(
            Selection selection,
            IProjectionFieldHandler handler) =>
            new ProjectionSelection(
                handler,
                selection.DeclaringType,
                selection.Field,
                selection.SyntaxNode,
                selection.ResolverPipeline,
                selection.ResponseName,
                selection.Arguments,
                selection.IncludeConditions?.FirstOrDefault(),
                selection.IsInternal);
    }
}
