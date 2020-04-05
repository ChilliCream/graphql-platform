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
            services.AddGraphQL(
                SchemaBuilder.New()
                    .AddDocumentFromString("type Query { a: String }")
                    .Use(next => context => Task.CompletedTask));

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
            Action action = () => ((IServiceCollection)null).AddGraphQL(schema);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchema_SchemaNull()
        {
            // arrange
            // act
            Action action = () => new ServiceCollection().AddGraphQL(default(Schema));

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
            services.AddGraphQL(schema);

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
            Action action = () => ((IServiceCollection)null).AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            services.AddGraphQL(
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
            Action action = () => ((IServiceCollection)null).AddGraphQL(new Func<IServiceProvider, ISchema>(s => schema));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactory_SchemaFactoryNull()
        {
            // arrange
            // act
            Action action = () => new ServiceCollection().AddGraphQL(
                default(Func<IServiceProvider, ISchema>)
            );

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaFactoryBuilder_ServiceNull()
        {
            // arrange
            var schema = Schema.Create(c => c.Options.StrictValidation = false);

            // act
            Action action = () => ((IServiceCollection)null).AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            services.AddGraphQL(
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
            Action action = () => ((IServiceCollection)null).AddGraphQL(
                new Action<ISchemaConfiguration>(c => { }));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesConfigure_ConfigureNull()
        {
            // arrange
            // act
            Action action = () => new ServiceCollection().AddGraphQL(
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
            services.AddGraphQL(
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
            Action action = () => ((IServiceCollection)null).AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            services.AddGraphQL(
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
            Action action = () => ((IServiceCollection)null).AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            services.AddGraphQL(
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
            Action action = () => ((IServiceCollection)null).AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            services.AddGraphQL(
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
            Action action = () => ((IServiceCollection)null).AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            services.AddGraphQL(
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
            Action action = () => ((IServiceCollection)null).AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            services.AddGraphQL(
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
            Action action = () => ((IServiceCollection)null).AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            services.AddGraphQL(
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
            Action action = () => ((IServiceCollection)null).AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
                default(IQueryExecutor));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddGraphQL_ServicesSchemaConfigureBuilder_ServiceNull()
        {
            // arrange
            // act
            Action action = () => ((IServiceCollection)null).AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            Action action = () => new ServiceCollection().AddGraphQL(
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
            services.AddGraphQL(
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
