using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution
{
    public class ScalarValidationExceptions
    {
        [Fact(Skip = "Test is not ready.")]
        public async Task Exec()
        {
            var schema = Schema.Create(t =>
            {
                t.RegisterQueryType<QueryType>();
            });

            IQueryExecuter executer = schema.MakeExecutable();
            IExecutionResult result = await executer.ExecuteAsync(
                "{ stringToName(name: \"  \") }");

            result.Snapshot();
        }

        [Fact(Skip = "Test is not ready.")]
        public async Task Exec2()
        {
            var schema = Schema.Create(t =>
            {
                t.RegisterQueryType<QueryType>();
            });

            IQueryExecuter executer = schema.MakeExecutable();
            IExecutionResult result = await executer.ExecuteAsync(
                "{ nameToString(name: \"  \") }");

            result.Snapshot();
        }


        public class Query
        {
            public string StringToName(string name) => name;

            public string NameToString(string name) => name;
        }

        public class QueryType
            : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Field(t => t.StringToName(default))
                    .Argument("name", a => a.Type<NameType>())
                    .Type<StringType>();

                descriptor.Field(t => t.NameToString(default))
                    .Argument("name", a => a.Type<StringType>())
                    .Type<NameType>();
            }
        }
    }
}
