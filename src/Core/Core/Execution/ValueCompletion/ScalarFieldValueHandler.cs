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
                    SerializeAndCompleteValue(completionContext, serializable);
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

        private void SerializeAndCompleteValue(
            IFieldValueCompletionContext completionContext,
            ISerializableType serializable)
        {
            try
            {
                if (TryConvertToScalarValue(
                    completionContext.Converter,
                    completionContext.Type,
                    completionContext.Value,
                    out object value))
                {
                    value = serializable.Serialize(value);
                    completionContext.IntegrateResult(value);
                }
                else
                {
                    completionContext.ReportError(
                        "The internal resolver value could not be " +
                        "converted to a valid value of " +
                        $"`{completionContext.Type.TypeName()}`.");
                }
            }
            catch (ScalarSerializationException ex)
            {
                completionContext.ReportError(ex.Message);
            }
            catch (Exception)
            {
                completionContext.ReportError(
                    "Undefined field serialization error.");
            }
        }

        private static bool TryConvertToScalarValue(
            ITypeConversion converter,
            IType type,
            object value,
            out object scalarValue)
        {
            try
            {
                if (value is null)
                {
                    scalarValue = value;
                    return true;
                }

                if (type is IHasClrType leafType
                    && !leafType.ClrType.IsInstanceOfType(value))
                {
                    return converter.TryConvert(
                        typeof(object),
                        leafType.ClrType,
                        value,
                        out scalarValue);
                }

                scalarValue = value;
                return true;
            }
            catch
            {
                scalarValue = null;
                return false;
            }
        }
    }
}
