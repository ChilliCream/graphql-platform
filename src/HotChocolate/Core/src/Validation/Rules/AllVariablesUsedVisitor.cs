using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Validation.Rules
{
    /// <summary>
    /// All variables defined by an operation must be used in that operation
    /// or a fragment transitively included by that operation.
    ///
    /// Unused variables cause a validation error.
    ///
    /// http://spec.graphql.org/June2018/#sec-All-Variables-Used
    ///
    /// AND
    ///
    /// Variables are scoped on a per‐operation basis. That means that
    /// any variable used within the context of an operation must be defined
    /// at the top level of that operation
    ///
    /// http://spec.graphql.org/June2018/#sec-All-Variable-Uses-Defined
    /// </summary>
    internal sealed class AllVariablesUsedVisitor : DocumentValidatorVisitor
    {
        public AllVariablesUsedVisitor()
            : base(new SyntaxVisitorOptions
                {
                    VisitDirectives = true,
                    VisitArguments = true
                })
        {
        }

        protected override ISyntaxVisitorAction Enter(
            VariableDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.Unused.Add(node.Variable.Name.Value);
            context.Declared.Add(node.Variable.Name.Value);
            return Skip;
        }

        protected override ISyntaxVisitorAction Enter(
            VariableNode node,
            IDocumentValidatorContext context)
        {
            context.Used.Add(node.Name.Value);
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.Unused.Clear();
            context.Used.Clear();
            context.Declared.Clear();
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.Unused.ExceptWith(context.Used);
            context.Used.ExceptWith(context.Declared);

            if (context.Unused.Count > 0)
            {
                context.Errors.Add(context.VariableNotUsed(node));
            }

            if (context.Used.Count > 0)
            {
                context.Errors.Add(context.VariableNotDeclared(node));
            }

            return Continue;
        }
    }
}
