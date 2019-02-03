using System;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddGraphQL_ServicesSchema_ServiceNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null, schema);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchema_SchemaNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(), default(Schema));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactory_ServiceNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null, new Func<IServiceProvider, ISchema>(s => schema));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactory_SchemaFactoryNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(Func<IServiceProvider, ISchema>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactoryBuilder_ServiceNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null,
                new Func<IServiceProvider, ISchema>(s => schema),
                new Func<IQueryExecutionBuilder, IQueryExecutionBuilder>(
                    b => b));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactoryBuilder_SchemaFactoryNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(Func<IServiceProvider, ISchema>),
                new Func<IQueryExecutionBuilder, IQueryExecutionBuilder>(
                    b => b));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactoryBuilder_BuilderNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                new Func<IServiceProvider, ISchema>(s => schema),
                default(Func<IQueryExecutionBuilder, IQueryExecutionBuilder>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesConfigure_ServiceNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null, 
                new Action<ISchemaConfiguration>(c => { }));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesConfigure_SchemaFactoryNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(Action<ISchemaConfiguration>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
