using System;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
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
        public void AddGraphQL_ServicesConfigure_ConfigureNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(Action<ISchemaConfiguration>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesConfigureBuilder_ServiceNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null,
                new Action<ISchemaConfiguration>(c => { }),
                new Func<IQueryExecutionBuilder, IQueryExecutionBuilder>(
                    b => b));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesConfigureBuilder_ConfigureNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(Action<ISchemaConfiguration>),
                new Func<IQueryExecutionBuilder, IQueryExecutionBuilder>(
                    b => b));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesConfigureBuilder_BuilderNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                new Action<ISchemaConfiguration>(c => { }),
                default(Func<IQueryExecutionBuilder, IQueryExecutionBuilder>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaSdlConfigure_ServiceNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null,
                "type Query { a: String }",
                new Action<ISchemaConfiguration>(c => { }));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaSdlConfigure_SchemaSdlNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(string),
                new Action<ISchemaConfiguration>(c => { }));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaSdlConfigure_SchemaSdlEmpty()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                string.Empty,
                new Action<ISchemaConfiguration>(c => { }));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaSdlConfigure_ConfigureNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                "type Query { a: String }",
                default(Action<ISchemaConfiguration>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaSdlConfigureBld_ServiceNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null,
                "type Query { a: String }",
                new Action<ISchemaConfiguration>(c => { }),
                new Func<IQueryExecutionBuilder, IQueryExecutionBuilder>(
                    b => b));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaSdlConfigureBld_SchemaSdlNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(string),
                new Action<ISchemaConfiguration>(c => { }),
                new Func<IQueryExecutionBuilder, IQueryExecutionBuilder>(
                    b => b));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaSdlConfigureBld_SchemaSdlEmpty()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                string.Empty,
                new Action<ISchemaConfiguration>(c => { }),
                new Func<IQueryExecutionBuilder, IQueryExecutionBuilder>(
                    b => b));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaSdlConfigureBld_ConfigureNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                "type Query { a: String }",
                default(Action<ISchemaConfiguration>),
                new Func<IQueryExecutionBuilder, IQueryExecutionBuilder>(
                    b => b));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaSdlConfigureBld_BuilderNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                "type Query { a: String }",
                new Action<ISchemaConfiguration>(c => { }),
                default(Func<IQueryExecutionBuilder, IQueryExecutionBuilder>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaOptions_ServiceNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null,
                schema,
                new QueryExecutionOptions());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaOptions_SchemaNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(Schema),
                new QueryExecutionOptions());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaOptions_OptionsNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                schema,
                default(IQueryExecutionOptionsAccessor));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
