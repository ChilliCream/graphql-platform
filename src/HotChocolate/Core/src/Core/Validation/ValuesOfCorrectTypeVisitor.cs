using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class ValuesOfCorrectTypeVisitor
        : InputObjectFieldVisitorBase
    {
        private readonly HashSet<ObjectValueNode> _visited =
            new HashSet<ObjectValueNode>();
        private readonly Dictionary<NameString, DirectiveType> _directives;
        private readonly Dictionary<NameString, ITypeNode> _variables =
            new Dictionary<NameString, ITypeNode>();


        public ValuesOfCorrectTypeVisitor(ISchema schema)
            : base(schema)
        {
            _directives = schema.DirectiveTypes.ToDictionary(t => t.Name);
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode operation,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (VariableDefinitionNode variableDefinition in
                operation.VariableDefinitions)
            {
                _variables[variableDefinition.Variable.Name.Value] =
                    variableDefinition.Type;

                if (!variableDefinition.DefaultValue.IsNull())
                {
                    IType type = ConvertTypeNodeToType(variableDefinition.Type);
                    IValueNode defaultValue = variableDefinition.DefaultValue;

                    if (type is IInputType inputType
                        && !IsInstanceOfType(inputType, defaultValue))
                    {
                        Errors.Add(new ValidationError(
                            "The specified value type of variable " +
                            $"`{variableDefinition.Variable.Name.Value}` " +
                            "does not match the variable type.",
                            variableDefinition));
                    }
                    else if (defaultValue is ObjectValueNode ov
                        && type is InputObjectType iot)
                    {
                        VisitObjectValue(iot, ov);
                    }
                }
            }

            base.VisitOperationDefinition(operation, path);
        }

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is IComplexOutputType ct
                && ct.Fields.TryGetField(field.Name.Value, out IOutputField f))
            {
                foreach (ArgumentNode argument in field.Arguments)
                {
                    VisitArgument(f.Arguments, argument);
                }
            }

            base.VisitField(field, type, path);
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            if (_directives.TryGetValue(
                directive.Name.Value,
                out DirectiveType d))
            {
                foreach (ArgumentNode argument in directive.Arguments)
                {
                    VisitArgument(d.Arguments, argument);
                }
            }

            base.VisitDirective(directive, path);
        }

        protected override void VisitObjectValue(
            InputObjectType type,
            ObjectValueNode objectValue)
        {
            if (_visited.Add(objectValue))
            {
                foreach (ObjectFieldNode fieldValue in objectValue.Fields)
                {
                    if (type.Fields.TryGetField(fieldValue.Name.Value,
                        out InputField field))
                    {
                        if (IsInstanceOfType(field.Type, fieldValue.Value))
                        {
                            if (fieldValue.Value is ObjectValueNode ov
                                && field.Type.NamedType() is InputObjectType it)
                            {
                                VisitObjectValue(type, objectValue);
                            }
                        }
                        else
                        {
                            Errors.Add(new ValidationError(
                                "The specified value type of field " +
                                $"`{fieldValue.Name.Value}` " +
                                "does not match the field type.",
                                fieldValue));
                        }
                    }
                }
            }
        }

        private void VisitArgument(
            IFieldCollection<IInputField> argumentFields,
            ArgumentNode argument)
        {
            if (argumentFields.TryGetField(argument.Name.Value,
                out IInputField argumentField)
                && !(argument.Value is VariableNode)
                && !IsInstanceOfType(argumentField.Type, argument.Value))
            {
                Errors.Add(new ValidationError(
                    "The specified argument value " +
                    "does not match the argument type.\n" +
                    $"Argument: `{argument.Name.Value}`\n" +
                    $"Value: `{argument.Value}`",
                    argument));
            }
        }

        private IType ConvertTypeNodeToType(ITypeNode typeNode)
        {
            if (typeNode is NonNullTypeNode nntn)
            {
                return new NonNullType(ConvertTypeNodeToType(nntn.Type));
            }

            if (typeNode is ListTypeNode ltn)
            {
                return new ListType(ConvertTypeNodeToType(ltn.Type));
            }

            if (typeNode is NamedTypeNode ntn)
            {
                return Schema.GetType<INamedType>(ntn.Name.Value);
            }

            throw new NotSupportedException();
        }

        private bool IsInstanceOfType(
            IInputType inputType,
            IValueNode value)
        {
            if (value is VariableNode v
                && _variables.TryGetValue(v.Name.Value, out ITypeNode t))
            {
                return IsTypeCompatible(inputType, t);
            }

            IInputType internalType = inputType;

            if (internalType.IsNonNullType())
            {
                internalType = (IInputType)internalType.InnerType();
            }

            if (internalType is ListType listType
                && listType.ElementType is IInputType elementType
                && value is ListValueNode list)
            {
                for (int i = 0; i < list.Items.Count; i++)
                {
                    if (!IsInstanceOfType(elementType, list.Items[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            return internalType.IsInstanceOfType(value);
        }

        private bool IsTypeCompatible(IType left, ITypeNode right)
        {
            if (left is NonNullType leftNonNull)
            {
                if (right is NonNullTypeNode rightNonNull)
                {
                    return IsTypeCompatible(
                        leftNonNull.Type,
                        rightNonNull.Type);
                }
                return false;
            }

            if (right is NonNullTypeNode nonNull)
            {
                return IsTypeCompatible(left, nonNull.Type);
            }

            if (left is ListType leftList)
            {
                if (right is ListTypeNode rightList)
                {
                    return IsTypeCompatible(
                        leftList.ElementType,
                        rightList.Type);
                }
                return false;
            }

            if (left is INamedType leftNamedType
                && right is NamedTypeNode rightNamedType)
            {
                return leftNamedType.Name.Equals(rightNamedType.Name.Value);
            }

            return false;
        }
    }
}
