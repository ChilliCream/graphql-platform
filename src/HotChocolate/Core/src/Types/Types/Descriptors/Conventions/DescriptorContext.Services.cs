using System.Text;
using HotChocolate.Internal;
using HotChocolate.Utilities;
using Microsoft.Extensions.ObjectPool;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public sealed partial class DescriptorContext
{
    private sealed class ServiceHelper
    {
        private readonly IServiceProvider _schemaServices;
        private readonly IServiceProvider? _appServices;

        public ServiceHelper(IServiceProvider schemaServices)
        {
            _schemaServices = schemaServices ??
                throw new ArgumentNullException(nameof(schemaServices));
            _appServices = _schemaServices.GetService<IApplicationServiceProvider>();
        }

        public IReadOnlyList<IParameterExpressionBuilder> GetParameterExpressionBuilders()
        {
            var builders = new List<IParameterExpressionBuilder>();

            var service = _schemaServices.GetService<IEnumerable<IParameterExpressionBuilder>>();

            if (service is not null)
            {
                builders.AddRange(service);
            }

            if (_appServices is not null)
            {
                service = _appServices.GetService<IEnumerable<IParameterExpressionBuilder>>();

                if (service is not null)
                {
                    builders.AddRange(service);
                }
            }

            return builders;
        }

        public ITypeConverter GetTypeConverter()
        {
            ITypeConverter? converter;

            if (_appServices is not null)
            {
                converter = _appServices.GetService<ITypeConverter>();

                if (converter is not null)
                {
                    return converter;
                }
            }

            converter = _schemaServices.GetService<ITypeConverter>();

            if (converter is not null)
            {
                return converter;
            }

            return DefaultTypeConverter.Default;
        }

        public InputFormatter GetInputFormatter(ITypeConverter converter)
        {
            InputFormatter? formatter;

            if (_appServices is not null)
            {
                formatter = _appServices.GetService<InputFormatter>();

                if (formatter is not null)
                {
                    return formatter;
                }
            }

            formatter = _schemaServices.GetService<InputFormatter>();

            if (formatter is not null)
            {
                return formatter;
            }

            return new InputFormatter(converter);
        }

        public InputParser GetInputParser(ITypeConverter converter)
        {
            InputParser? parser;

            if (_appServices is not null)
            {
                parser = _appServices.GetService<InputParser>();

                if (parser is not null)
                {
                    return parser;
                }
            }

            parser = _schemaServices.GetService<InputParser>();

            if (parser is not null)
            {
                return parser;
            }

            return new InputParser(converter);
        }

        public ObjectPool<StringBuilder> GetStringBuilderPool()
        {
            ObjectPool<StringBuilder>? pool;

            if (_appServices is not null)
            {
                pool = _appServices.GetService<ObjectPool<StringBuilder>>();

                if (pool is not null)
                {
                    return pool;
                }
            }

            pool = _schemaServices.GetService<ObjectPool<StringBuilder>>();

            if (pool is not null)
            {
                return pool;
            }

            return new NoOpStringBuilderPool();
        }

        public T? GetService<T>() where T : class
        {
            if (_appServices is not null)
            {
                var service = _appServices.GetService<T?>();

                if (service is not null)
                {
                    return service;
                }
            }

            return _schemaServices.GetService<T?>();
        }
    }
}
