using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Data.Projections
{
    public class ProjectionOptimizer : ISelectionOptimizer
    {
        private readonly IProjectionProvider _convention;

        public ProjectionOptimizer(
            IProjectionProvider convention)
        {
            _convention = convention;
        }

        public void OptimizeSelectionSet(SelectionOptimizerContext context)
        {
            var processedFields = new HashSet<string>();
            while (!processedFields.SetEquals(context.Fields.Keys))
            {
                var fieldsToProcess = new HashSet<string>(context.Fields.Keys);
                fieldsToProcess.ExceptWith(processedFields);
                foreach (var field in fieldsToProcess)
                {
                    context.Fields[field] =
                        _convention.RewriteSelection(context, context.Fields[field]);
                    processedFields.Add(field);
                }
            }
        }

        public bool AllowFragmentDeferral(
            SelectionOptimizerContext context,
            InlineFragmentNode fragment)
        {
            return false;
        }

        public bool AllowFragmentDeferral(
            SelectionOptimizerContext context,
            FragmentSpreadNode fragmentSpread,
            FragmentDefinitionNode fragmentDefinition)
        {
            return false;
        }
    }
}
