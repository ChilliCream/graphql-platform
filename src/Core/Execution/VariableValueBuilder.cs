using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    // http://facebook.github.io/graphql/draft/#sec-Coercing-Variable-Values
    internal sealed class VariableValueBuilder
    {
        private readonly ISchema _schema;
        private readonly OperationDefinitionNode _operation;

        public VariableValueBuilder(ISchema schema, OperationDefinitionNode operation)
        {
            _schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            _operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
        }

        // <summary>
        /// Creates the concrete variable values
        /// from the variables passed in by the user
        /// and the defaults defined in the query.
        /// </summary>
        /// <param name="variableValues">
        /// The variable values that were passed in by the user.
        /// </param>
        /// <returns>
        /// Returns the coerced variable values converted
        /// to their .net counterparts.
        /// </returns>/
        public VariableCollection CreateValues(
            IReadOnlyDictionary<string, IValueNode> variableValues)
        {
            IReadOnlyDictionary<string, IValueNode> values =
                variableValues ?? new Dictionary<string, IValueNode>();
            Dictionary<string, object> coercedValues =
                new Dictionary<string, object>();

            foreach (VariableDefinitionNode variableDefinition in
                _operation.VariableDefinitions)
            {
                Variable variable = CreateVariable(variableDefinition);
                CoerceVariableValue(values, ref variable);
                coercedValues[variable.Name] = variable.ParseLiteral();
            }

            return new VariableCollection(coercedValues);
        }

        private Variable CreateVariable(
            VariableDefinitionNode variableDefinition)
        {
            string variableName = variableDefinition.Variable.Name.Value;
            IType variableType = GetType(variableDefinition.Type);

            if (variableType is IInputType type)
            {
                return new Variable(
                    variableName, type,
                    variableDefinition.DefaultValue);
            }

            throw new QueryException(new VariableError(
                $"The variable type ({variableType.ToString()}) " +
                "must be an input object type.",
                variableName));
        }

        private void CoerceVariableValue(
            IReadOnlyDictionary<string, IValueNode> variableValues,
            ref Variable variable)
        {
            if (!variableValues.TryGetValue(variable.Name,
                out IValueNode variableValue))
            {
                variableValue = variable.DefaultValue ?? new NullValueNode();
            }

            variable = variable.WithValue(variableValue);

            CheckForNullValueViolation(in variable);
            CheckForInvalidValueType(in variable);
        }

        private void CheckForNullValueViolation(in Variable variable)
        {
            if (variable.Type.IsNonNullType() && IsNulValue(variable.Value))
            {
                throw new QueryException(new VariableError(
                    "The variable value cannot be null.",
                    variable.Name));
            }
        }

        private void CheckForInvalidValueType(in Variable variable)
        {
            if (!variable.Type.IsInstanceOfType(variable.Value))
            {
                throw new QueryException(new VariableError(
                    "The variable value is not of the correct type.",
                    variable.Name));
            }
        }

        private IType GetType(ITypeNode typeNode)
        {
            if (typeNode is NonNullTypeNode nonNullType)
            {
                return new NonNullType(GetType(nonNullType.Type));
            }

            if (typeNode is ListTypeNode listType)
            {
                return new ListType(GetType(listType.Type));
            }

            if (typeNode is NamedTypeNode namedType)
            {
                return _schema.GetType<INamedType>(namedType.Name.Value);
            }

            throw new NotSupportedException(
                "The type node kind is not supported.");
        }

        private static bool IsNulValue(IValueNode valueNode)
        {
            return valueNode == null || valueNode is NullValueNode;
        }

        private readonly struct Variable
        {
            public Variable(string name, IInputType type,
                IValueNode defaultValue)
                : this(name, type, defaultValue, defaultValue)
            {
            }

            public Variable(string name, IInputType type,
                IValueNode defaultValue, IValueNode value)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException(
                        "Variables cannot have an empty name.",
                        nameof(name));
                }

                Name = name;
                Type = type
                    ?? throw new ArgumentNullException(nameof(type));
                DefaultValue = defaultValue;
                Value = value;
            }

            public string Name { get; }

            public IInputType Type { get; }

            public IValueNode DefaultValue { get; }

            public IValueNode Value { get; }

            public Variable WithValue(IValueNode value)
            {
                return new Variable(Name, Type, DefaultValue, value);
            }

            public object ParseLiteral()
            {
                try
                {
                    return Type.ParseLiteral(Value);
                }
                catch (ArgumentException ex)
                {
                    throw new QueryException(new VariableError(
                        ex.Message, Name));
                }
            }
        }
    }
}
