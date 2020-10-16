using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotChocolate.Data.Neo4j
{
    class CypherParameter
    {
        private readonly string? _value;

        CypherParameter(string value)
        {
            if(value is null)
            {
                _value = "null";
            }

            _value = value;
        }
        CypherParameter(Raw clause)
        {
            _value = clause.ToString();
        }

        public override string ToString()
        {
            return _value;
        }
    }
}
