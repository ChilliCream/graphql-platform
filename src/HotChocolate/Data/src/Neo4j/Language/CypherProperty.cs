namespace HotChocolate.Data.Neo4j
{
    class CypherProperty
    {
        private readonly string _key;
        private readonly CypherParameter _param;
        private readonly string _statementOperator;

        CypherProperty(string key, CypherParameter param, string statementOperator = ClauseOperator.Equal)
        {
            _key = key;
            _param = param;
            _statementOperator = statementOperator;
        }

        public string GetParameters()
        {
            return _param.ToString();
        }

        public override string ToString()
        {
            return $"{_key} {_statementOperator} {_param}";
        }
    }
}
