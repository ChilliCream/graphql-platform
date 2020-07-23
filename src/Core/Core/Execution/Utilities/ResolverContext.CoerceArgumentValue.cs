using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal partial class ResolverContext
        : IMiddlewareContext
    {
        public T Argument<T>(NameString name)
        {
            name.EnsureNotEmpty(nameof(name));

            PreCoerceArguments();

            if (_arguments.TryGetValue(name, out PreparedArgument argumentValue))
            {
                return CoerceArgumentValue<T>(name, argumentValue);
            }

            return default;
        }

        public ValueKind ArgumentKind(NameString name)
        {
            name.EnsureNotEmpty(nameof(name));

            PreCoerceArguments();

            if (_arguments.TryGetValue(name, out PreparedArgument argumentValue))
            {
                return argumentValue.Kind ?? ValueKind.Unknown;
            }

            return ValueKind.Null;
        }

        private T CoerceArgumentValue<T>(
            string name,
            PreparedArgument argumentValue)
        {
            object value = argumentValue.Value;

            if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
            {
                IValueNode literal = (argumentValue.ValueLiteral == null)
                    ? argumentValue.Type.ParseValue(value)
                    : argumentValue.ValueLiteral;

                return (T)VariableToValueRewriter.Rewrite(
                    literal,
                    argumentValue.Type,
                    _executionContext.Variables,
                    _executionContext.Converter);
            }

            if (argumentValue.ValueLiteral != null)
            {
                IValueNode literal = VariableToValueRewriter.Rewrite(
                    argumentValue.ValueLiteral,
                    argumentValue.Type,
                    _executionContext.Variables,
                    _executionContext.Converter);

                value = argumentValue.Type.ParseLiteral(literal);
            }

            if (value is null)
            {
                return default;
            }

            if (value is T resolved)
            {
                return resolved;
            }

            if (TryConvertValue(
                argumentValue.Type.ClrType,
                value, out resolved))
            {
                return resolved;
            }

            if (typeof(T).IsClass
                && (value is IReadOnlyDictionary<string, object>
                || value is IReadOnlyList<object>))
            {
                var dictToObjConverter =
                    new DictionaryToObjectConverter(Converter);
                return (T)dictToObjConverter.Convert(value, typeof(T));
            }

            IError error = ErrorBuilder.New()
                .SetMessage(string.Format(
                    CultureInfo.InvariantCulture,
                    CoreResources.ResolverContext_ArgumentConversion,
                    name,
                    argumentValue.Type.ClrType.FullName,
                    typeof(T).FullName))
                .SetPath(Path)
                .AddLocation(FieldSelection)
                .Build();

            throw new QueryException(error);
        }

        private bool TryConvertValue<T>(
            Type type,
            object value,
            out T converted)
        {
            if (Converter.TryConvert(
                type, typeof(T),
                value, out object c))
            {
                converted = (T)c;
                return true;
            }

            converted = default;
            return false;
        }

        private void PreCoerceArguments()
        {
            if (_arguments is null)
            {
                List<IError> errors = null;

                if (!_fieldSelection.Arguments.TryCoerceArguments(
                    _executionContext.Variables,
                    error =>
                    {
                        if (errors is null)
                        {
                            errors = new List<IError>();
                        }
                        errors.Add(error.WithPath(Path));
                    },
                    out _arguments))
                {
                    throw new QueryException(errors);
                }
            }
        }
    }
}
