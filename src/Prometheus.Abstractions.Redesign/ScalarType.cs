namespace Prometheus.Types
{
    public class ScalarType
    {

    }

    /*
    
    export type GraphQLScalarTypeConfig<TInternal, TExternal> = {
  name: string,
  description?: ?string,
  astNode?: ?ScalarTypeDefinitionNode,
  serialize: (value: mixed) => ?TExternal,
  parseValue?: (value: mixed) => ?TInternal,
  parseLiteral?: (
    valueNode: ValueNode,
    variables: ?ObjMap<mixed>,
  ) => ?TInternal,
};
    
     */

    public class ScalarTypeConfig
    {
        public string Name { get; }
        public string Description { get; }

        public IReadOnlyDictionary<string, Field>
    }

    /*
    serialize: gets invoked when serializing the result to send it back to a client.
    parseValue: gets invoked to parse client input that was passed through variables.
    parseLiteral: gets invoked to parse client input that was passed inline in the query.
     */
}