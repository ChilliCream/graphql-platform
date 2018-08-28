using System;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ScalarFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext completionContext,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (completionContext.Type.IsScalarType()
                || completionContext.Type.IsEnumType())
            {
                if (completionContext.Type is ISerializableType serializable)
                {
                    try
                    {
                        completionContext.IntegrateResult(
                            serializable.Serialize(completionContext.Value));
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
    }
}
