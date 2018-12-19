using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class AllVariableUsagesAreAllowedVisitor
        : QueryVisitorErrorBase
    {
        private readonly Dictionary<string, DirectiveType> _directives;
        private readonly Dictionary<string, VariableDefinitionNode> _variableDefinitions
            = new Dictionary<string, VariableDefinitionNode>();
        private readonly List<VariableUsage> _variablesUsages =
            new List<VariableUsage>();

        public AllVariableUsagesAreAllowedVisitor(ISchema schema)
            : base(schema)
        {
            _directives = schema.DirectiveTypes.ToDictionary(t => t.Name);
        }

        protected override void VisitFragmentDefinitions(
            IEnumerable<FragmentDefinitionNode> fragmentDefinitions,
            ImmutableStack<ISyntaxNode> path)
        {
            // we only want to visit framnet definitions that are used by
            // operations. So, we skip visiting all fragment definitions.
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode operation,
            ImmutableStack<ISyntaxNode> path)
        {
            base.VisitOperationDefinition(operation, path);

            foreach (VariableDefinitionNode variable in
                operation.VariableDefinitions)
            {
                _variableDefinitions[variable.Variable.Name.Value] = variable;
            }

            FindVariableUsageErrors();

            _variableDefinitions.Clear();
            _variablesUsages.Clear();
        }

        protected override void VisitField(
           FieldNode field,
           IType type,
           ImmutableStack<ISyntaxNode> path)
        {
            if (type is IComplexOutputType ct
                && ct.Fields.TryGetField(field.Name.Value, out IOutputField of))
            {
                ValidateArguments(of.Arguments, field.Arguments);
            }
            base.VisitField(field, type, path);
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            if (_directives.TryGetValue(directive.Name.Value, out DirectiveType d))
            {
                ValidateArguments(d.Arguments, directive.Arguments);
            }
            base.VisitDirective(directive, path);
        }

        private void ValidateArguments(
            IFieldCollection<IInputField> arguments,
            IEnumerable<ArgumentNode> argumentNodes)
        {
            foreach (ArgumentNode argument in argumentNodes)
            {
                if (argument.Value is VariableNode vn
                    && arguments.TryGetField(argument.Name.Value,
                        out IInputField f))
                {
                    _variablesUsages.Add(
                        new VariableUsage(f, argument, vn));
                }
            }
        }

        private void FindVariableUsageErrors()
        {
            foreach (VariableUsage variableUsage in _variablesUsages)
            {
                if (_variableDefinitions.TryGetValue(
                    variableUsage.Name,
                    out VariableDefinitionNode variableDefinition)
                    && !IsVariableUsageAllowed(
                        variableDefinition, variableUsage))
                {
                    string variableName =
                        variableDefinition.Variable.Name.Value;

                    Errors.Add(new ValidationError(
                        $"The variable `{variableName}` type is not " +
                        "compatible with the type of the argument " +
                        $"`{variableUsage.InputField.Name}`.\r\n" +
                        $"Expected type: `{variableUsage.Type.TypeName()}`.",
                        variableUsage.Argument, variableDefinition));
                }
            }
        }

        // http://facebook.github.io/graphql/June2018/#IsVariableUsageAllowed()
        private bool IsVariableUsageAllowed(
            VariableDefinitionNode variableDefinition,
            VariableUsage variableUsage)
        {
            if (variableUsage.Type.IsNonNullType()
                && !variableDefinition.Type.IsNonNullType())
            {
                if (variableDefinition.DefaultValue.IsNull()
                    && variableUsage.InputField.DefaultValue.IsNull())
                {
                    return false;
                }

                return AreTypesCompatible(
                    variableDefinition.Type,
                    variableUsage.Type.NullableType());
            }

            return AreTypesCompatible(
                variableDefinition.Type,
                variableUsage.Type);
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

        private readonly struct VariableUsage
        {
            public VariableUsage(
                IInputField inputField,
                ArgumentNode argument,
                VariableNode variable)
            {
                InputField = inputField
                    ?? throw new ArgumentNullException(nameof(inputField));
                Argument = argument
                    ?? throw new ArgumentNullException(nameof(argument));
                Variable = variable
                    ?? throw new ArgumentNullException(nameof(variable));
            }

            public string Name => Variable.Name.Value;

            public IType Type => InputField.Type;

            public IInputField InputField { get; }

            public ArgumentNode Argument { get; }

            public VariableNode Variable { get; }
        }
    }
}
