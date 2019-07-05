using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    internal partial class ResolverContext
        : IMiddlewareContext
    {
        public T Argument<T>(NameString name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_arguments.TryGetValue(name, out ArgumentValue argumentValue))
            {
                return CoerceArgumentValue<T>(name, argumentValue);
            }

            return default;
        }

        // TODO : simplify
        private T CoerceArgumentValue<T>(
            string name,
            ArgumentValue argumentValue)
        {
            object value = argumentValue.Value;

            if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
            {
                IValueNode literal =  (argumentValue.Literal == null)
                    ? argumentValue.Type.ParseValue(value)
                    : argumentValue.Literal;

                return (T)VariableToValueRewriter.Rewrite(
                    literal,
                    argumentValue.Type,
                    _executionContext.Variables,
                    _executionContext.Converter);
            }

            if (argumentValue.Literal != null)
            {
                IValueNode literal = VariableToValueRewriter.Rewrite(
                    argumentValue.Literal,
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
    }
}
