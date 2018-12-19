using System;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal class ScalarFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext completionContext,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (completionContext.Type.IsLeafType())
            {
                if (completionContext.Type is ISerializableType serializable)
                {
                    try
                    {
                        object value = Normalize(
                            completionContext.Converter,
                            completionContext.Type,
                            completionContext.Value);

                        value = serializable.Serialize(value);

                        completionContext.IntegrateResult(value);
                    }
                    catch (ArgumentException ex)
                    {
                        completionContext.ReportError(ex.Message);
                    }
                    catch (Exception)
                    {
                        completionContext.ReportError(
                            "Undefined field serialization error.");
                    }
                }
                else
                {
                    completionContext.ReportError(
                        "Scalar types and enum types must be serializable.");
                }
            }
            else
            {
                nextHandler?.Invoke(completionContext);
            }
        }

        private object Normalize(
            ITypeConversion converter,
            IType type,
            object value)
        {
            if (value is null)
            {
                return value;
            }

            if (type is IHasClrType leafType
                && !leafType.ClrType.IsInstanceOfType(value))
            {
                return converter.Convert(
                    typeof(object), leafType.ClrType,
                    value);
            }

            return value;
        }
    }
}
