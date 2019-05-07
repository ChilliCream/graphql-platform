using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
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

        private T CoerceArgumentValue<T>(
            string name,
            ArgumentValue argumentValue)
        {
            if (argumentValue.Value is T value)
            {
                return value;
            }

            if (argumentValue.Value == null)
            {
                return default;
            }

            if (TryConvertValue(argumentValue, out value))
            {
                return value;
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
            ArgumentValue argumentValue,
            out T value)
        {
            if (Converter.TryConvert(
                argumentValue.Type.ClrType, typeof(T),
                argumentValue.Value, out object converted))
            {
                value = (T)converted;
                return true;
            }

            value = default;
            return false;
        }
    }
}
