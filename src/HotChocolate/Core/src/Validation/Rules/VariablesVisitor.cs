using System;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation.Rules
{
    /// <summary>
    /// If any operation defines more than one variable with the same name,
    /// it is ambiguous and invalid. It is invalid even if the type of the
    /// duplicate variable is the same.
    ///
    /// http://spec.graphql.org/June2018/#sec-Validation.Variables
    ///
    /// AND
    ///
    /// Variables can only be input types. Objects,
    /// unions, and interfaces cannot be used as inputs.
    ///
    /// http://spec.graphql.org/June2018/#sec-Variables-Are-Input-Types
    ///
    /// AND
    ///
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
    ///
    /// AND
    ///
    /// Variable usages must be compatible with the arguments
    /// they are passed to.
    ///
    /// Validation failures occur when variables are used in the context
    /// of types that are complete mismatches, or if a nullable type in a
    ///  variable is passed to a non‐null argument type.
    ///
    /// http://spec.graphql.org/June2018/#sec-All-Variable-Usages-are-Allowed
    /// </summary>
    internal sealed class VariablesVisitor
        : TypeDocumentValidatorVisitor
    {
        public VariablesVisitor()
            : base(new SyntaxVisitorOptions
            {
                VisitDirectives = true,
                VisitArguments = true
            })
        {
        }

        protected override ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Clear();
            context.Unused.Clear();
            context.Used.Clear();
            context.Declared.Clear();
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            VariableDefinitionNode node,
            IDocumentValidatorContext context)
        {
            string variableName = node.Variable.Name.Value;

            context.Unused.Add(variableName);
            context.Declared.Add(variableName);

            if (context.Schema.TryGetType(
                    node.Type.NamedType().Name.Value, out INamedType type) &&
                !type.IsInputType())
            {
                context.Errors.Add(context.VariableNotInputType(node, variableName));
            }

            if (!context.Names.Add(variableName))
            {
                context.Errors.Add(context.VariableNameNotUnique(node, variableName));
            }
            return Skip;
        }

        protected override ISyntaxVisitorAction Enter(
            VariableNode node,
            IDocumentValidatorContext context)
        {
            context.Used.Add(node.Name.Value);

            if (!context.IsInError.PeekOrDefault(true))
            {
                ISyntaxNode parent = context.Path.Peek();
                IValueNode? defaultValue;

                switch (parent.Kind)
                {
                    case NodeKind.Argument:
                    case NodeKind.ObjectField:
                        defaultValue = context.InputFields.Peek().DefaultValue;
                        break;

                    default:
                        defaultValue = null;
                        break;
                }

                if (context.Variables.TryGetValue(
                    node.Name.Value,
                    out VariableDefinitionNode? variableDefinition)
                    && !IsVariableUsageAllowed(variableDefinition, context.Types.Peek(), defaultValue))
                {
                    context.Errors.Add(ErrorHelper.VariableIsNotCompatible(
                        context, node, variableDefinition));
                }
            }

            return Skip;
        }

        protected override ISyntaxVisitorAction Enter(
            ListValueNode node,
            IDocumentValidatorContext context)
        {
            if (context.Types.TryPeek(out IType? type) && type.IsListType())
            {
                context.Types.Push(type.ElementType());
                return Continue;
            }
            return Break;
        }

        protected override ISyntaxVisitorAction Leave(
            ListValueNode node,
            IDocumentValidatorContext context)
        {
            context.Types.Pop();
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

        // http://facebook.github.io/graphql/June2018/#IsVariableUsageAllowed()
        private bool IsVariableUsageAllowed(
            VariableDefinitionNode variableDefinition,
            IType locationType,
            IValueNode? locationDefault)
        {
            if (locationType.IsNonNullType()
                && !variableDefinition.Type.IsNonNullType())
            {
                if (variableDefinition.DefaultValue.IsNull()
                    && locationDefault.IsNull())
                {
                    return false;
                }

                return AreTypesCompatible(
                    variableDefinition.Type,
                    locationType.NullableType());
            }

            return AreTypesCompatible(
                variableDefinition.Type,
                locationType);
        }

        // http://facebook.github.io/graphql/June2018/#AreTypesCompatible()
        private bool AreTypesCompatible(
            ITypeNode variableType,
            IType locationType)
        {
            if (locationType.IsNonNullType())
            {
                if (variableType.IsNonNullType())
                {
                    return AreTypesCompatible(
                        variableType.InnerType(),
                        locationType.InnerType());
                }
                return false;
            }

            if (variableType.IsNonNullType())
            {
                return AreTypesCompatible(
                    variableType.InnerType(),
                    locationType);
            }

            if (locationType.IsListType())
            {
                if (variableType.IsListType())
                {
                    return AreTypesCompatible(
                        variableType.InnerType(),
                        locationType.InnerType());
                }
                return false;
            }

            if (variableType.IsListType())
            {
                return false;
            }

            if (variableType is NamedTypeNode vn
                && locationType is INamedType lt)
            {
                return string.Equals(
                    vn.Name.Value,
                    lt.Name,
                    StringComparison.Ordinal);
            }

            return false;
        }
    }
}
