using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Validation.Options;

namespace HotChocolate.Validation.Rules
{
    internal sealed class MaxExecutionDepthVisitor : TypeDocumentValidatorVisitor
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
                context.Errors.Add(context.MaxOperationComplexity(
                    node, _options.MaxAllowedExecutionDepth.Value, context.Max));
                return Break;
            }

            return base.Leave(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            if (context.Count < context.OutputFields.Count + 1)
            {
                context.Count += context.OutputFields.Count + 1;
            }

            if (IntrospectionFields.TypeName.Equals(node.Name.Value))
            {
                return Skip;
            }
            else if (context.Types.TryPeek(out IType type) &&
                type.NamedType() is IComplexOutputType ot &&
                ot.Fields.TryGetField(node.Name.Value, out IOutputField of))
            {
                context.OutputFields.Push(of);
                context.Types.Push(of.Type);
                return Continue;
            }
            else
            {
                context.UnexpectedErrorsDetected = true;
                return Skip;
            }
        }

        protected override ISyntaxVisitorAction Leave(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            context.Types.Pop();
            context.OutputFields.Pop();
            return Continue;
        }
    }
}
