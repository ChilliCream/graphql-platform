using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;

namespace Zeus.Execution
{

    /*
       public class OperationCompiler
           : IOperationCompiler
       {
           public ICompiledOperation Compile(ISchema schema, QueryDocument queryDocument, string operationName)
           {
               OperationDefinition operationDefinition = GetOperation(queryDocument, operationName);





           }

           private CompiledOperation CreateOperation(ISchema schema, OperationDefinition operation)
           {
               if (schema.ObjectTypes.TryGetValue(operation.Type.ToString(), out var typeDefinition))
               {

               }
           }




           private static OperationDefinition GetOperation(QueryDocument queryDocument, string name)
           {
               if (string.IsNullOrEmpty(name))
               {
                   if (document.Operations.Count == 1)
                   {
                       return document.Operations.Values.First();
                   }
                   throw new Exception("TODO: Query Exception");
               }
               else
               {
                   if (document.Operations.TryGetValue(name, out var operation))
                   {
                       return operation;
                   }
                   throw new Exception("TODO: Query Exception");
               }
           }
       }
        */

    public interface IOperationOptimizer
    {
        IOptimizedOperation Optimize(ISchema schema, QueryDocument queryDocument, string operationName);
    }

    public interface IVariableCollection
    {
        T GetVariable<T>(string variableName);
    }

   
































}