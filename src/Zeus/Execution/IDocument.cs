using GraphQLParser.AST;

namespace Zeus.Execution
{
    public interface IDocument
    {
        GraphQLOperationDefinition GetOperation(string name);
    }
}