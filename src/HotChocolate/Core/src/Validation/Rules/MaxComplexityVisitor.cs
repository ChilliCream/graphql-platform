using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Validation.Options;

namespace HotChocolate.Validation.Rules
{
    internal sealed class MaxComplexityVisitor : TypeDocumentValidatorVisitor
    {
        private readonly IMaxComplexityOptionsAccessor _options;

        public MaxComplexityVisitor(IMaxComplexityOptionsAccessor options)
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

            if (_options.MaxAllowedComplexity.HasValue &&
                _options.MaxAllowedComplexity < context.Max)
            {
                context.Errors.Add(context.MaxOperationComplexity(
                    node, _options.MaxAllowedComplexity.Value, context.Max));
                return Break;
            }

            return base.Leave(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            if (IntrospectionFields.TypeName.Equals(node.Name.Value))
            {
                context.Count += _options.ComplexityCalculation.Invoke(
                    TypeNameField,
                    node,
                    null,
                    context.OutputFields.Count + 1,
                    context.Path.Count + 1,
                    GetVariable,
                    _options);
                return Skip;
            }
            else if (context.Types.TryPeek(out IType type) &&
                type.NamedType() is IComplexOutputType ot &&
                ot.Fields.TryGetField(node.Name.Value, out IOutputField of))
            {
                context.Count += _options.ComplexityCalculation.Invoke(
                    of,
                    node,
                    of.Directives["cost"].FirstOrDefault()?.ToObject<CostDirective>(),
                    context.OutputFields.Count + 1,
                    context.Path.Count + 1,
                    GetVariable,
                    _options);

                context.OutputFields.Push(of);
                context.Types.Push(of.Type);
                return Continue;
            }
            else
            {
                context.UnexpectedErrorsDetected = true;
                return Skip;
            }

            object? GetVariable(string key)
            {
                if (context.VariableValues.TryGetValue(key, out object? value))
                {
                    return value;
                }
                return null;
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
