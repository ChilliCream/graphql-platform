using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Stitching.Properties;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Delegation
{
    public class RemoteExecutorBuilder
    {
        private string _schemaName;
        private string _schema;
        private readonly List<Type> _scalarTypes = new List<Type>();
        private readonly List<ScalarType> _scalarTypeInstances =
            new List<ScalarType>();

        public RemoteExecutorBuilder SetSchemaName(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    StitchingResources.SchemaName_EmptyOrNull,
                    nameof(schemaName));
            }

            _schemaName = schemaName;
            return this;
        }

        public RemoteExecutorBuilder SetSchema(string schema)
        {
            if (string.IsNullOrEmpty(schema))
            {
                throw new ArgumentException(
                    StitchingResources.SchemaName_EmptyOrNull,
                    nameof(schema));
            }

            _schema = schema;
            return this;
        }

        public RemoteExecutorBuilder AddScalarType<T>()
            where T : ScalarType
        {
            _scalarTypes.Add(typeof(T));
            return this;
        }

        public RemoteExecutorBuilder AddScalarType(ScalarType scalarType)
        {
            if (scalarType == null)
            {
                throw new ArgumentNullException(nameof(scalarType));
            }

            _scalarTypeInstances.Add(scalarType);
            return this;
        }

        public RemoteExecutorBuilder AddScalarType(Type scalarType)
        {
            if (scalarType == null)
            {
                throw new ArgumentNullException(nameof(scalarType));
            }

            if (!typeof(ScalarType).IsAssignableFrom(scalarType))
            {
                throw new ArgumentException(
                    StitchingResources.ScalarType_InvalidBaseType,
                    nameof(scalarType));
            }

            _scalarTypes.Add(scalarType);
            return this;
        }

        public IRemoteExecutorAccessor Build()
        {
            if (string.IsNullOrEmpty(_schemaName))
            {
                throw new InvalidOperationException(
                    StitchingResources.RemoteExecutorBuilder_NoSchemaName);
            }

            if (string.IsNullOrEmpty(_schema))
            {
                throw new InvalidOperationException(
                    StitchingResources.RemoteExecutorBuilder_NoSchema);
            }

            ISchema schema = Schema.Create(
                _schema,
                c =>
                {
                    c.Options.StrictValidation = false;

                    foreach (Type type in _scalarTypes)
                    {
                        c.RegisterType(type);
                    }

                    foreach (ScalarType instance in _scalarTypeInstances)
                    {
                        c.RegisterType(instance);
                    }

                    c.UseNullResolver();
                });

            return new RemoteExecutorAccessor(
                _schemaName,
                schema.MakeExecutable(b =>
                    b.UseQueryDelegationPipeline(_schemaName)));
        }

        public static RemoteExecutorBuilder New() =>
            new RemoteExecutorBuilder();
    }
}
