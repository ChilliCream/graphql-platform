using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class AllVariableUsagesAreAllowedVisitor
        : QueryVisitorErrorBase
    {
        private readonly Dictionary<string, VariableDefinitionNode> _variableDefinitions
            = new Dictionary<string, VariableDefinitionNode>();
        private readonly List<VariableNode> _variablesUsages =
            new List<VariableNode>();

        public AllVariableUsagesAreAllowedVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode operation,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (VariableDefinitionNode variable in
                operation.VariableDefinitions)
            {
                _variableDefinitions[variable.Variable.Name.Value] = variable;
            }

            base.VisitOperationDefinition(operation, path);


            _variableDefinitions.Clear();
            _variablesUsages.Clear();
        }

        protected override void VisitField(
           FieldNode field,
           IType type,
           ImmutableStack<ISyntaxNode> path)
        {
            ValidateArguments(field, field.Arguments);
            base.VisitField(field, type, path);
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            ValidateArguments(directive, directive.Arguments);
            base.VisitDirective(directive, path);
        }

        private void ValidateArguments(
            ISyntaxNode node,
            IEnumerable<ArgumentNode> arguments)
        {
            foreach (ArgumentNode argument in arguments)
            {

            }
        }

        private void FindVariableUsageErrors()
        {
            foreach (VariableNode variableUsage in _variablesUsages)
            {
                if (_variableDefinitions.TryGetValue(
                    variableUsage.Name.Value,
                    out VariableDefinitionNode expectedVariableType))
                {

                }
            }
        }

        private bool AreTypesCompatible(
            ITypeNode variableType,
            ITypeNode locationType)
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
                && locationType is NamedTypeNode ln)
            {
                return string.Equals(
                    vn.Name.Value,
                    ln.Name.Value,
                    StringComparison.Ordinal);
            }

            return false;
        }
    }
}
