using System;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ScalarFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext context,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (context.Type.IsScalarType() || context.Type.IsEnumType())
            {
                if (context.Type is ISerializableType serializable)
                {
                    try
                    {
                        context.IntegrateResult(serializable.Serialize(context.Value));
                    }
                    catch (ArgumentException ex)
                    {
                        context.ReportError(ex.Message);
                    }
                    catch (Exception)
                    {
                        context.ReportError(
                            "Undefined field serialization error.");
                    }
                }
                else
                {
                    context.ReportError(
                        "Scalar types and enum types must be serializable.");
                }
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }
    }
}
