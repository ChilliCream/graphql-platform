using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Client;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Stitching.Introspection;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Properties;
using HotChocolate.Stitching.Utilities;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    public partial class StitchingBuilder
        : IStitchingBuilder
    {
        private OrderedDictionary<NameString, LoadSchemaDocument> _schemas =
            new OrderedDictionary<NameString, LoadSchemaDocument>();
        private readonly List<LoadSchemaDocument> _extensions =
            new List<LoadSchemaDocument>();
        private readonly List<MergeTypeHandler> _mergeHandlers =
            new List<MergeTypeHandler>();
        private readonly List<Action<ISchemaConfiguration>> _schemaConfigs =
            new List<Action<ISchemaConfiguration>>();
        private readonly List<Action<IQueryExecutionBuilder>> _execConfigs =
            new List<Action<IQueryExecutionBuilder>>();
        private readonly List<RenameTypeDescriptor> _renameTypeDescriptors =
            new List<RenameTypeDescriptor>();
        private readonly List<RenameFieldDescriptor> _renameFieldDescriptors =
            new List<RenameFieldDescriptor>();
        private readonly List<RenameTypeDescriptor> _ignoreTypeDescriptors =
            new List<RenameTypeDescriptor>();
        private readonly List<RenameFieldDescriptor> _ignoreFieldDescriptors =
            new List<RenameFieldDescriptor>();
        private IQueryExecutionOptionsAccessor _options;
        private bool _ignoreRootTypes;


        public IStitchingBuilder AddSchema(
            NameString name,
            LoadSchemaDocument loadSchema)
        {
            if (loadSchema == null)
            {
                throw new ArgumentNullException(nameof(loadSchema));
            }

            name.EnsureNotEmpty(nameof(name));

            _schemas.Add(name, loadSchema);

            return this;
        }

        public IStitchingBuilder AddExtensions(
            LoadSchemaDocument loadExtensions)
        {
            if (loadExtensions == null)
            {
                throw new ArgumentNullException(nameof(loadExtensions));
            }

            _extensions.Add(loadExtensions);

            return this;
        }

        public IStitchingBuilder AddMergeHandler(MergeTypeHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _mergeHandlers.Add(handler);

            return this;
        }

        public IStitchingBuilder AddSchemaConfiguration(
            Action<ISchemaConfiguration> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _schemaConfigs.Add(configure);
            return this;
        }

        public IStitchingBuilder AddExecutionConfiguration(
            Action<IQueryExecutionBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _execConfigs.Add(configure);
            return this;
        }

        public IStitchingBuilder SetExecutionOptions(
            IQueryExecutionOptionsAccessor options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options;
            return this;
        }

        public IStitchingBuilder IgnoreRootTypes()
        {
            throw new NotImplementedException();
        }

        public IStitchingBuilder IgnoreRootTypes(NameString schemaName)
        {
            throw new NotImplementedException();
        }

        public IStitchingBuilder IgnoreType(NameString typeName)
        {
            throw new NotImplementedException();
        }

        public IStitchingBuilder IgnoreType(NameString schemaName, NameString typeName)
        {
            throw new NotImplementedException();
        }

        public IStitchingBuilder IgnoreField(NameString schemaName, FieldReference field)
        {
            throw new NotImplementedException();
        }

        public IStitchingBuilder RenameType(NameString schemaName, NameString typeName, NameString newName)
        {
            throw new NotImplementedException();
        }

        public IStitchingBuilder RenameField(NameString schemaName, FieldReference field, NameString newName)
        {
            throw new NotImplementedException();
        }



        public void Populate(IServiceCollection serviceCollection)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.TryAddSingleton(services =>
                StitchingFactory.Create(this, services));

            serviceCollection.TryAddScoped(services =>
                services.GetRequiredService<StitchingFactory>()
                    .CreateStitchingContext(services));

            if (!serviceCollection.Any(d =>
                d.ImplementationType == typeof(RemoteQueryBatchOperation)))
            {
                serviceCollection.AddScoped<
                    IBatchOperation,
                    RemoteQueryBatchOperation>();
            }

            serviceCollection.TryAddSingleton(services =>
                services.GetRequiredService<StitchingFactory>()
                    .CreateStitchedQueryExecuter());

            serviceCollection.TryAddSingleton(services =>
                services.GetRequiredService<IQueryExecutor>()
                    .Schema);
        }

        public static StitchingBuilder New() => new StitchingBuilder();

        private class RenameTypeDescriptor
        {
            private RenameTypeDescriptor(
                NameString schemaName,
                NameString typeName,
                NameString newName)
            {
                SchemaName = schemaName;
                TypeName = typeName;
                NewName = newName;
            }

            public NameString SchemaName { get; }
            public NameString TypeName { get; }
            public NameString NewName { get; }
        }

        private class RenameFieldDescriptor
        {
            private RenameFieldDescriptor(
                NameString schemaName,
                FieldReference field,
                NameString newName)
            {
                SchemaName = schemaName;
                Field = field;
                NewName = newName;
            }
            public NameString SchemaName { get; }
            public FieldReference Field { get; }
            public NameString NewName { get; }
        }

        private class IgnoreTypeDescriptor
        {
            private IgnoreTypeDescriptor(
                NameString schemaName,
                NameString typeName)
            {
                SchemaName = schemaName;
                TypeName = typeName;
            }

            public NameString SchemaName { get; }
            public NameString TypeName { get; }
        }

        private class IgnoreFieldDescriptor
        {
            private IgnoreFieldDescriptor(
                NameString schemaName,
                FieldReference field)
            {
                SchemaName = schemaName;
                Field = field;

            }
            public NameString SchemaName { get; }
            public FieldReference Field { get; }
        }
    }
}
