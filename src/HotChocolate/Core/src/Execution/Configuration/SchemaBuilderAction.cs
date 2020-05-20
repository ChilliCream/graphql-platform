using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Configuration
{
    public readonly struct SchemaBuilderAction
    {
        public SchemaBuilderAction(Action<SchemaBuilder> action)
        {
            Action = action;
            AsyncAction = default;
        }

        public SchemaBuilderAction(Func<SchemaBuilder, CancellationToken, ValueTask> asyncAction)
        {
            Action = default;
            AsyncAction = asyncAction;
        }

        public Action<SchemaBuilder>? Action { get; }

        public Func<SchemaBuilder, CancellationToken, ValueTask>? AsyncAction { get; }
    }



/*



    public class Test
    {
        public void Bar(IServiceCollection services)
        {
            // .ConfigureSchema(builder => builder.Use())
            // .ConfigureExecution(builder => builder.Use())

            services
                .AddGraphQL("Foo")
                .AddQueryType<Foo>()
                .Use()
                .UseField()
                .AddMd5DocumentHashProvider()
                .UsePersistedQueryPipeline()
                .SetSchemaOptions(options)
                .SetExecutionOptions(options)
                .ModifyExecutionOptions(o => o.StrictValidation = true);


            services
                .AddGraphQL("Foo")
                .AddQueryType<Foo>();

            services
                .AddGraphQL("Bar")
                .AddQueryType<Bar>();

            services
                .AddGraphQL()
                .AddQueryType<Foo>();

            services
                .AddGraphQL() => // IGraphQLBuilder 
                .AddQueryType<Foo>()
                .UseDefaultRequestPipeline()
                .UsePersistedQueryRequestPipeline()
                .UseActivePersistedQueryRequestPipeline()
                .UseRequest(next => context =>
                {

                })
                .UseField(next => context =>
                {

                })
                .MapField("Query", "foo", next => context =>
                {

                })
                .ModifySchemaOptions(o => o.kkkk = false)
                .ModifyExecutionOptions(o => o.kkkk = false)
                .ModifyStitchingOptions(o => o.kkkk = false)
                .AddSchemaFromHttp("fooo") => ConfigureSchema()
                .ConfigureSchema()
                .ConfigureExecution();



        }
    }

    */




}