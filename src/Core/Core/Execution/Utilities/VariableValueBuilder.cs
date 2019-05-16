using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    // http://facebook.github.io/graphql/draft/#sec-Coercing-Variable-Values
    internal sealed class VariableValueBuilder
    {
        private readonly static Dictionary<string, object> _empty =
            new Dictionary<string, object>();
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
            var values = variableValues ?? _empty;
            var coercedValues = new Dictionary<string, object>();

            if (_operation.VariableDefinitions.Count > 0)
            {
                foreach (VariableDefinitionNode variableDefinition in
                    _operation.VariableDefinitions)
                {
                    Variable variable = CreateVariable(variableDefinition);
                    variable = CoerceVariableValue(
                        variableDefinition, values, variable);
                    coercedValues[variable.Name] = variable.Value;
                }
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

            throw new QueryException(ErrorBuilder.New()
                .SetMessage(string.Format(
                    CultureInfo.InvariantCulture,
                    TypeResources.VariableValueBuilder_InputType,
                    variableName,
                    TypeVisualizer.Visualize(variableType)))
                .AddLocation(variableDefinition)
                .Build());
        }

        private Variable CoerceVariableValue(
            VariableDefinitionNode variableDefinition,
            IReadOnlyDictionary<string, object> variableValues,
            Variable variable)
        {
            var value = variableValues.TryGetValue(
                variable.Name, out var rawValue)
                ? Normalize(variableDefinition, variable, rawValue)
                : variable.DefaultValue;

            variable = variable.WithValue(value);

            if (variable.Type.IsNonNullType() && variable.Value is null)
            {
                throw new QueryException(ErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.VariableValueBuilder_NonNull,
                        variable.Name,
                        TypeVisualizer.Visualize(variable.Type)))
                    .AddLocation(variableDefinition)
                    .Build());
            }

            InputTypeNonNullCheck.CheckForNullValueViolation(
                variable.Type, variable.Value,
                message => ErrorBuilder.New()
                    .SetMessage(message)
                    .AddLocation(variableDefinition)
                    .Build());
            CheckForInvalidValueType(variableDefinition, variable);

            return variable;
        }

        private object Normalize(
            VariableDefinitionNode variableDefinition,
            Variable variable,
            object rawValue)
        {
            object value = rawValue;

            if (value is null || value is NullValueNode)
            {
                return null;
            }

            if (value is IValueNode literal)
            {
                CheckForInvalidValueType(
                    variableDefinition, variable, literal);
                value = variable.Type.ParseLiteral(literal);
            }

            value = DeserializeValue(variable.Type, value);
            value = EnsureClrTypeIsCorrect(variable.Type, value);

            return value;
        }

        private object DeserializeValue(IInputType type, object value)
        {
            if (type.IsLeafType()
                && type.NamedType() is ISerializableType serializable
                && serializable.TryDeserialize(value, out object deserialized))
            {
                return deserialized;
            }

            if (type.IsListType() && value is IList<object>)
            {
                return _inputTypeConverter.Convert(value, type);
            }

            if (type.IsInputObjectType()
                && value is IDictionary<string, object>)
            {
                return _inputTypeConverter.Convert(value, type);
            }

            return value;
        }

        private object EnsureClrTypeIsCorrect(IHasClrType type, object value)
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

        private static void CheckForInvalidValueType(
            VariableDefinitionNode variableDefinition,
            Variable variable)
        {
            if (variable.Value != null
                && !variable.Type.IsInstanceOfType(variable.Value))
            {
                throw new QueryException(ErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.VariableValueBuilder_InvalidValue,
                        variable.Name))
                    .AddLocation(variableDefinition)
                    .Build());
            }
        }

        private static void CheckForInvalidValueType(
            VariableDefinitionNode variableDefinition,
            Variable variable,
            IValueNode value)
        {
            if (!variable.Type.IsInstanceOfType(value))
            {
                throw new QueryException(ErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.VariableValueBuilder_InvalidValue,
                        variable.Name))
                    .AddLocation(variableDefinition)
                    .Build());
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
                TypeResources.VariableValueBuilder_NodeKind);
        }

        private ref struct Variable
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
                        TypeResources.VariableValueBuilder_VarNameEmpty,
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
