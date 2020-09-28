using System.Threading.Tasks;
using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddGraphQL_ServicesSchemaBuilder()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                SchemaBuilder.New()
                    .AddDocumentFromString("type Query { a: String }")
                    .Use(next => context => default(ValueTask)));

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();
        }

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
        public void AddGraphQL_ServicesSchema()
        {
            // arrange
            var services = new ServiceCollection();
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                schema);

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();

        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigure_ServiceNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null,
                schema,
                new Action<IQueryExecutionBuilder>(c => { }));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigure_SchemaNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(Schema),
                new Action<IQueryExecutionBuilder>(c => { }));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigure_ConfigureNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                schema,
                default(Action<IQueryExecutionBuilder>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigure()
        {
            // arrange
            var services = new ServiceCollection();
            var schema = Schema.Create(c => c.Options.StrictValidation = false);
            var cfg = new Action<IQueryExecutionBuilder>(
                c => c.UseDefaultPipeline());

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                schema,
                cfg);

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();
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
                new Action<IQueryExecutionBuilder>(c => { }));

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
                new Action<IQueryExecutionBuilder>(c => { }));

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
                default(Action<IQueryExecutionBuilder>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactoryBuilder()
        {
            // arrange
            var services = new ServiceCollection();
            var schema = Schema.Create(c => c.Options.StrictValidation = false);
            var cfg = new Action<IQueryExecutionBuilder>(
                c => c.UseDefaultPipeline());

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                sp => schema,
                cfg);

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();
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
        public void AddGraphQL_ServicesConfigure()
        {
            // arrange
            var services = new ServiceCollection();
            var schemaCfg = new Action<ISchemaConfiguration>(
                c => c.Options.StrictValidation = false);

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                schemaCfg);

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();
        }

        [Fact]
        public void AddGraphQL_ServicesConfigureBuilder_ServiceNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null,
                new Action<ISchemaConfiguration>(c => { }),
                new Action<IQueryExecutionBuilder>(c => { }));

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
                new Action<IQueryExecutionBuilder>(c => { }));

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
                default(Action<IQueryExecutionBuilder>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesConfigureBuilder()
        {
            // arrange
            var services = new ServiceCollection();
            var schemaCfg = new Action<ISchemaConfiguration>(
                c => c.Options.StrictValidation = false);
            var cfg = new Action<IQueryExecutionBuilder>(
                c => c.UseDefaultPipeline());

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                schemaCfg,
                cfg);

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();
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
        public void AddGraphQL_ServicesSchemaSdlConfigure()
        {
            // arrange
            var services = new ServiceCollection();
            string schema = "type Query { a: String }";
            var schemaCfg = new Action<ISchemaConfiguration>(
                c => c.Options.StrictValidation = false);

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                schema,
                schemaCfg);

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();
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
                new Action<IQueryExecutionBuilder>(c => { }));

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
                new Action<IQueryExecutionBuilder>(c => { }));

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
                new Action<IQueryExecutionBuilder>(c => { }));

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
                new Action<IQueryExecutionBuilder>(c => { }));

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
                default(Action<IQueryExecutionBuilder>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaSdlConfigureBld()
        {
            // arrange
            var services = new ServiceCollection();
            string schema = "type Query { a: String }";
            var schemaCfg = new Action<ISchemaConfiguration>(
                c => c.Options.StrictValidation = false);
            var cfg = new Action<IQueryExecutionBuilder>(
                c => c.UseDefaultPipeline());

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                schema,
                schemaCfg,
                cfg);

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();
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

        [Fact]
        public void AddGraphQL_ServicesSchemaOptions()
        {
            // arrange
            var services = new ServiceCollection();
            var schema = Schema.Create(c => c.Options.StrictValidation = false);
            var options = new QueryExecutionOptions();

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                schema,
                options);

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactoryOptions_ServiceNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null,
                sp => schema,
                new QueryExecutionOptions());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactoryOptions_SchemaNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(Func<IServiceProvider, ISchema>),
                new QueryExecutionOptions());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactoryOptions_OptionsNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                sp => schema,
                default(IQueryExecutionOptionsAccessor));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactoryOptions()
        {
            // arrange
            var services = new ServiceCollection();
            var schema = Schema.Create(c => c.Options.StrictValidation = false);
            var options = new QueryExecutionOptions();

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                sp => schema,
                options);

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigOptions_ServiceNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null,
                new Action<ISchemaConfiguration>(c => { }),
                new QueryExecutionOptions());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigOptions_SchemaNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(Action<ISchemaConfiguration>),
                new QueryExecutionOptions());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigOptions_OptionsNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                new Action<ISchemaConfiguration>(c => { }),
                default(IQueryExecutionOptionsAccessor));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigOptions()
        {
            // arrange
            var services = new ServiceCollection();
            var options = new QueryExecutionOptions();

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                c => c.Options.StrictValidation = false,
                options);

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();
        }

        [Obsolete("Use different overload.", true)]
        [Fact]
        public void AddGraphQL_ServicesQueryExecutor_ServiceNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null,
                schema.MakeExecutable());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Obsolete("Use different overload.", true)]
        [Fact]
        public void AddGraphQL_ServicesQueryExecutor_ExecutorNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                default(IQueryExecutor));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigureBuilder_ServiceNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                null,
                "type Query { a: String }",
                new Action<ISchemaConfiguration>(c => { }),
                new Action<IQueryExecutionBuilder>(c => { }));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigureBuilder_SchemaNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                null,
                new Action<ISchemaConfiguration>(c => { }),
                new Action<IQueryExecutionBuilder>(c => { }));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigureBuilder_ConfigureNull()
        {
            // arrange
            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                "type Query { a: String }",
                default(Action<ISchemaConfiguration>),
                new Action<IQueryExecutionBuilder>(c => { }));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigureBuilder_BuilderNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ServiceCollectionExtensions.AddGraphQL(
                new ServiceCollection(),
                "type Query { a: String }",
                new Action<ISchemaConfiguration>(c => { }),
                default(Action<IQueryExecutionBuilder>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigureBuilder()
        {
            // arrange
            var services = new ServiceCollection();
            var schema = "type Query { a: String }";
            var schemaCfg = new Action<ISchemaConfiguration>(
                c => c.Options.StrictValidation = false);
            var cfg = new Action<IQueryExecutionBuilder>(
                c => c.UseDefaultPipeline());

            // act
            ServiceCollectionExtensions.AddGraphQL(
                services,
                schema,
                schemaCfg,
                cfg);

            // assert
            services.Select(t => ReflectionUtils.GetTypeName(t.ServiceType))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray()
                .MatchSnapshot();
        }
    }
}
