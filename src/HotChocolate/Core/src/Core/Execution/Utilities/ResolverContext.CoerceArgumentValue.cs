using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
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
        public IServiceProvider Services => throw new NotImplementedException();

        public IType ValueType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public T Argument<T>(NameString name)
        {
            name.EnsureNotEmpty(nameof(name));

            if (_arguments.TryGetValue(name, out ArgumentValue argumentValue))
            {
                EnsureNoError(argumentValue);
                return CoerceArgumentValue<T>(name, argumentValue);
            }

            return default;
        }

        public ValueKind ArgumentKind(NameString name)
        {
            name.EnsureNotEmpty(nameof(name));

            if (_arguments.TryGetValue(name, out ArgumentValue argumentValue))
            {
                EnsureNoError(argumentValue);
                return argumentValue.Kind ?? ValueKind.Unknown;
            }

            return ValueKind.Null;
        }

        private void EnsureNoError(ArgumentValue argumentValue)
        {
            if (argumentValue.Error != null)
            {
                throw new QueryException(argumentValue.Error);
            }
        }

        // TODO : simplify
        private T CoerceArgumentValue<T>(string name, ArgumentValue argumentValue)
        {
            object value = argumentValue.Value;

            if (value is T a)
            {
                return a;
            }

            if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
            {
                IValueNode literal = (argumentValue.Literal == null)
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

            if (TryConvertValue(value.GetType(), value, out resolved))
            {
                return resolved;
            }

            if (value is IReadOnlyDictionary<string, object>
                || value is IReadOnlyList<object>)
            {
                var dictToObjConverter = new DictionaryToObjectConverter(Converter);
                if (typeof(T).IsInterface)
                {
                    object o = dictToObjConverter.Convert(value, argumentValue.Type.ClrType);
                    if (o is T c)
                    {
                        return c;
                    }
                }
                else
                {
                    return (T)dictToObjConverter.Convert(value, typeof(T));
                }
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

        private bool TryConvertValue<T>(Type type, object value, out T converted)
        {
            if (Converter.TryConvert(type, typeof(T), value, out object c))
            {
                converted = (T)c;
                return true;
            }

            converted = default;
            return false;
        }

        public T ArgumentValue<T>(NameString name)
        {
            throw new NotImplementedException();
        }

        public T ArgumentLiteral<T>(NameString name) where T : IValueNode
        {
            throw new NotImplementedException();
        }

        public Optional<T> ArgumentOptional<T>(NameString name)
        {
            throw new NotImplementedException();
        }

        ValueTask<T> IMiddlewareContext.ResolveAsync<T>()
        {
            throw new NotImplementedException();
        }
    }
}
