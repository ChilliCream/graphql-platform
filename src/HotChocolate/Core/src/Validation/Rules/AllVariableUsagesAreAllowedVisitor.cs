using System;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Variable usages must be compatible with the arguments
    /// they are passed to.
    ///
    /// Validation failures occur when variables are used in the context
    /// of types that are complete mismatches, or if a nullable type in a
    ///  variable is passed to a non‐null argument type.
    ///
    /// http://spec.graphql.org/June2018/#sec-All-Variable-Usages-are-Allowed
    /// </summary>
    internal sealed class AllVariableUsagesAreAllowedVisitor : TypeDocumentValidatorVisitor
    {
        protected override ISyntaxVisitorAction Enter(
            VariableDefinitionNode node,
            IDocumentValidatorContext context) =>
            Skip;

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

        protected override ISyntaxVisitorAction Enter(
            VariableNode node,
            IDocumentValidatorContext context)
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
                string variableName = variableDefinition.Variable.Name.Value;

                context.Errors.Add(
                    ErrorBuilder.New()
                        .SetMessage(
                            $"The variable `{variableName}` is not compatible " +
                            "with the type of the current location.")
                        .AddLocation(node)
                        .SetPath(context.CreateErrorPath())
                        .SetExtension("variable", variableName)
                        .SetExtension("variableType", variableDefinition.Type.ToString())
                        .SetExtension("locationType", context.Types.Peek().Visualize())
                        .SpecifiedBy("sec-All-Variable-Usages-are-Allowed")
                        .Build());
            }
            return Skip;
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
