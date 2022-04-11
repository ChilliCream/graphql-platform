using System;
using System.Threading.Tasks;

namespace HotChocolate.Stitching.Types;

public class SchemaTransformer : ISchemaTransformer
{
    public ValueTask<ITransformationResult> Transform(IServiceDefinition serviceDefinition, SchemaTransformationOptions options)
    {
        //var transformationJournal = new HashSet<DirectiveNode>();

        //var visited = new HashSet<ISyntaxNode>();
        //var stack = new Stack<ISyntaxNode>();
        //stack.Push(serviceDefinition);

        //while (stack.Count > 0)
        //{
        //    var vertex = stack.Pop();

        //    if (visited.Contains(vertex))
        //        continue;

        //    visited.Add(vertex);

        //    foreach (var neighbor in graph.AdjacencyList[vertex])
        //        if (!visited.Contains(neighbor))
        //            stack.Push(neighbor);
        //}

        //return visited;
        //var source = new Stack<SchemaCoordinate>();
        //source.Push(new SchemaCoordinate(serviceDefinition.ServiceReference));

        //while (true)
        //{
        //    foreach (DocumentNode document in serviceDefinition.Documents)
        //    {

        //        document.GetNodes()
        //    }
        //}

        throw new NotImplementedException();
    }
}