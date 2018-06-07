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
                        context.SetResult(serializable.Serialize(context.Value));
                    }
                    catch (ArgumentException ex)
                    {
                        context.AddError(ex.Message);
                    }
                    catch (Exception)
                    {
                        context.AddError(
                            "Undefined field serialization error.");
                    }
                }
                else
                {
                    context.AddError(
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
