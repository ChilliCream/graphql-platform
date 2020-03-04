using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public interface ISchemaFactory
    {
        ISchema CreateSchema(string name);
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
        public IList<Action<ISchemaBuilder>> Options { get; } =
           new List<Action<ISchemaBuilder>>();
    }

    /*
        services.AddSchema("test")
            .AddQueryType<Foo>()
            .AddMutationType<Bar>()
            .UseDefaultPipeline()
            .WithExecutionOptions(options);
    */

    /*
        services.AddSchema("test",
            builder => builder.AddQueryType<Foo>()
                .AddMutationType<Bar>())
            .UseDefaultPipeline()
            .WithExecutionOptions(options);
    */
}
