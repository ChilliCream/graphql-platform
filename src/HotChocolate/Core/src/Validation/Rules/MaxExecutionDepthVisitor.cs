using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Validation.Options;

namespace HotChocolate.Validation.Rules
{
    internal sealed class MaxExecutionDepthVisitor : DocumentValidatorVisitor
    {
        private readonly IMaxExecutionDepthOptionsAccessor _options;

        public MaxExecutionDepthVisitor(IMaxExecutionDepthOptionsAccessor options)
        {
            _options = options;
        }

        protected override ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.Count = 0;
            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.Max = context.Count > context.Max ? context.Count : context.Max;

            if (_options.MaxAllowedExecutionDepth.HasValue &&
                _options.MaxAllowedExecutionDepth < context.Max)
            {
                context.Errors.Add(context.MaxExecutionDepth(
                    node, _options.MaxAllowedExecutionDepth.Value, context.Max));
                return Break;
            }

            return base.Leave(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            context.Fields.Push(node);

            if (context.Count < context.Fields.Count)
            {
                context.Count = context.Fields.Count;
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            context.Fields.Pop();
            return Continue;
        }
    }
}
