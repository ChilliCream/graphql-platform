using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Stitching
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
                    "The schema name mustn't be null or empty.",
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
                    "The schema mustn't be null or empty.",
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
                    $"scalarType must extend {typeof(ScalarType).FullName}.",
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
                    "Cannot build a remote executor without a schema name.");
            }

            if (string.IsNullOrEmpty(_schema))
            {
                throw new InvalidOperationException(
                    "Cannot build a remote executor without a schema.");
            }

            ISchema schema = Schema.Create(
                _schema,
                c =>
                {
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
