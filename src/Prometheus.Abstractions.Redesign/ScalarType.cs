using System;

namespace Prometheus.Types
{
    public delegate string SerializeValue(object value);
    public delegate object ParseLiteral(IValue value, GetVariableValue getVariableValue);
    public delegate object GetVariableValue(string variableName);
    
    public class ScalarType
    {
        private ScalarTypeConfig _config;
        public readonly SerializeValue _serialize;
        public readonly ParseLiteral _parseLiteral;

        public ScalarType(ScalarTypeConfig config)
        {
            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A scalar type name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            Name = config.Name;
            Description = config.Description;
            _serialize = config.Serialize;
            _parseLiteral = config.ParseLiteral;
        }

        public string Name { get; }

        public string Description { get; }

        /*
        TODO : Docu
   serialize: gets invoked when serializing the result to send it back to a client.
   parseValue: gets invoked to parse client input that was passed through variables.
   parseLiteral: gets invoked to parse client input that was passed inline in the query.
    */

        // .net native to external  
        public string Serialize(object value)
        {
            return _serialize(value);
        }

        // ast node to .net native
        public object ParseLiteral(IValue value, GetVariableValue getVariableValue)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (getVariableValue == null)
            {
                throw new ArgumentNullException(nameof(getVariableValue));
            }

            return _parseLiteral(value, getVariableValue);
        }
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
        public string Name { get; set; }

        public string Description { get; set; }

        public SerializeValue Serialize { get; set; }
        public ParseLiteral ParseLiteral { get; set; }
    }
}