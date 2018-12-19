using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    // http://facebook.github.io/graphql/draft/#sec-Coercing-Variable-Values
    internal sealed class VariableValueBuilder
    {
        private readonly ISchema _schema;
        private readonly OperationDefinitionNode _operation;
        private readonly ITypeConversion _converter;
        private readonly DictionaryToInputObjectConverter _inputTypeConverter;

        public VariableValueBuilder(
            ISchema schema,
            OperationDefinitionNode operation)
        {
            _schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            _operation = operation
                ?? throw new ArgumentNullException(nameof(operation));

            _converter = _schema.Services.GetTypeConversion();
            _inputTypeConverter = new DictionaryToInputObjectConverter(
                _converter);
        }

        /// <summary>
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
            IReadOnlyDictionary<string, object> variableValues)
        {
            IReadOnlyDictionary<string, object> values =
                variableValues ?? new Dictionary<string, object>();

            var coercedValues = new Dictionary<string, object>();

            foreach (VariableDefinitionNode variableDefinition in
                _operation.VariableDefinitions)
            {
                Variable variable = CreateVariable(variableDefinition);
                variable = CoerceVariableValue(values, variable);
                coercedValues[variable.Name] = variable.Value;
            }

            return new VariableCollection(_converter, coercedValues);
        }

        private Variable CreateVariable(
            VariableDefinitionNode variableDefinition)
        {
            var variableName = variableDefinition.Variable.Name.Value;
            IType variableType = GetType(variableDefinition.Type);

            if (variableType is IInputType type)
            {
                var defaultValue = variableDefinition.DefaultValue == null
                    ? null
                    : type.ParseLiteral(variableDefinition.DefaultValue);

                return new Variable(variableName, type, defaultValue);
            }

            throw new QueryException(QueryError.CreateVariableError(
                $"The variable type ({variableType.ToString()}) " +
                "must be an input object type.",
                variableName));
        }

        private Variable CoerceVariableValue(
            IReadOnlyDictionary<string, object> variableValues,
            Variable variable)
        {
            var value = variableValues.TryGetValue(
                variable.Name, out var rawValue)
                ? Normalize(variable, rawValue)
                : variable.DefaultValue;

            variable = variable.WithValue(value);

            CheckForNullValueViolation(variable);
            CheckForInvalidValueType(variable);

            return variable;
        }

        private object Normalize(Variable variable, object rawValue)
        {
            object value = rawValue;

            if (value is null || value is NullValueNode)
            {
                return null;
            }

            if (value is IValueNode literal)
            {
                CheckForInvalidValueType(variable, literal);
                value = variable.Type.ParseLiteral(literal);
            }

            value = DeserializeValue(variable.Type, value);
            value = EnsureClrTypeIsCorrect(variable.Type, value);

            return value;
        }

        private object DeserializeValue(IInputType type, object value)
        {
            if (value is IDictionary<string, object>
                || value is IList<object>)
            {
                return _inputTypeConverter.Convert(value, type);
            }
            else if (type.IsLeafType()
                && type.NamedType() is ISerializableType serializable
                && serializable.TryDeserialize(value, out object deserialized))
            {
                return deserialized;
            }

            return value;
        }

        private object EnsureClrTypeIsCorrect(IInputType type, object value)
        {
            if (type.ClrType != typeof(object)
                && value.GetType() != type.ClrType
                && _converter.TryConvert(value.GetType(),
                    type.ClrType, value,
                    out object converted))
            {
                return converted;
            }
            return value;
        }

        private void CheckForNullValueViolation(Variable variable)
        {
            if (variable.Type.IsNonNullType() && variable.Value is null)
            {
                throw new QueryException(QueryError.CreateVariableError(
                    "The variable value cannot be null.",
                    variable.Name));
            }
        }

        private void CheckForInvalidValueType(Variable variable)
        {
            if (variable.Value != null
                && !variable.Type.ClrType.IsInstanceOfType(variable.Value))
            {
                throw new QueryException(QueryError.CreateVariableError(
                    "The variable value is not of the correct type.",
                    variable.Name));
            }
        }

        private void CheckForInvalidValueType(
            Variable variable, IValueNode value)
        {
            if (!variable.Type.IsInstanceOfType(value))
            {
                throw new QueryException(QueryError.CreateVariableError(
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

        private class Variable
        {
            public Variable(
                string name,
                IInputType type,
                object defaultValue)
                : this(name, type, defaultValue, defaultValue)
            {
            }

            public Variable(
                string name,
                IInputType type,
                object defaultValue,
                object value)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException(
                        "Variables cannot have an empty name.",
                        nameof(name));
                }

                Name = name;
                Type = type ?? throw new ArgumentNullException(nameof(type));
                DefaultValue = defaultValue;
                Value = value;
            }

            public string Name { get; }

            public IInputType Type { get; }

            public object DefaultValue { get; }

            public object Value { get; }

            public Variable WithValue(object value)
            {
                return new Variable(Name, Type, DefaultValue, value);
            }
        }
    }
}
