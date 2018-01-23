using System;
using Zeus.Definitions;

namespace Zeus.Execution
{
    public class ResolverResult
    {
        public ResolverResult(string typeName, FieldDefinition field, object result)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Result = result;
        }

        public string TypeName { get; }
        public FieldDefinition Field { get; }
        public object Result { get; private set; }

        public void FinalizeResult()
        {
            if (Result is Func<object>)
            {
                Result = ((Func<object>)Result)();
            }
        }
    }
}