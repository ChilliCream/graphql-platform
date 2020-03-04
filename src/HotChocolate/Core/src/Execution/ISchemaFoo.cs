using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace HotChocolate.Execution
{
    public interface ISchemaFactory
    {
        ValueTask<ISchema> CreateSchemaAsync(
            string name,
            CancellationToken CancellationToken = default);
    }

    public interface ISchemaProvider
    {
        ValueTask<ISchema> GetSchemaAsync(
            string name,
            CancellationToken CancellationToken = default);
    }

    /// <summary>
    /// A builder for configuring named GraphQL schemas.
    /// </summary>
    public interface INamedSchemaBuilder
    {
        /// <summary>
        /// Gets the name of the schema configured by this builder.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the application service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }

    internal class DefaultNamedSchemaBuilder
        : INamedSchemaBuilder
    {
        public DefaultNamedSchemaBuilder(IServiceCollection services, string name)
        {
            Services = services;
            Name = name;
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }

    public class NamedSchemaOptions
    {
        public IList<Func<ISchemaBuilder, CancellationToken, ValueTask>> SchemaOptions { get; } =
           new List<Func<ISchemaBuilder, CancellationToken, ValueTask>>();

        public IList<Func<IQueryPipelineBuilder, CancellationToken, ValueTask>> ExecutorOptions { get; } =
           new List<Func<IQueryPipelineBuilder, CancellationToken, ValueTask>>();
    }

    /*
        services.AddSchema("test")
            .AddQueryType<Foo>()
            .AddMutationType<Bar>()
            .UseDefaultPipeline()
            .WithExecutionOptions(options);
    */

}
