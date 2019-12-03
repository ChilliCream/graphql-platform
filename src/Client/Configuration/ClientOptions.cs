using System.Linq;
using System.Collections.Generic;
using System;
using StrawberryShake.Serializers;
using System.Collections.Concurrent;

namespace StrawberryShake.Configuration
{
    public class ClientOptions
        : IClientOptions
    {
        private readonly ConcurrentDictionary<string, Configuration> _configurations =
            new ConcurrentDictionary<string, Configuration>();

        public IOperationFormatter GetOperationFormatter(string clientName)
        {
            return GetOrCreateConfiguration(clientName).OperationFormatter;
        }

        public IResultParserCollection GetResultParsers(string clientName)
        {
            return GetOrCreateConfiguration(clientName).ResultParsers;
        }

        public IValueSerializerCollection GetValueSerializers(string clientName)
        {
            return GetOrCreateConfiguration(clientName).ValueSerializers;
        }

        private Configuration GetOrCreateConfiguration(string clientName)
        {
            return _configurations.GetOrAdd(clientName, n => CreateConfiguration(n));
        }

        private static Configuration CreateConfiguration(string clientName)
        {
            ClientOptionsModifiers options = new ClientOptionsModifiers();

            if (options.ResultParsers.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The specified client `{clientName}` has no result parsers configured.");
            }

            if (options.OperationFormatter is null)
            {
                throw new InvalidOperationException(
                    $"The specified client `{clientName}` has no operations formatter configured.");
            }

            Dictionary<string, IValueSerializer> serializers =
                ValueSerializers.All.ToDictionary(t => t.Name);
            foreach (ConfigureValueSerializers configure in options.ValueSerializers)
            {
                configure(serializers);
            }

            var serializerCollection = new ValueSerializerCollection(serializers);
            var parsers = new Dictionary<Type, IResultParser>();
            foreach (ConfigureResultParsers configure in options.ResultParsers)
            {
                configure(serializerCollection, parsers);
            }

            var parserCollection = new ResultParserCollection(parsers);
            IOperationFormatter formatter = options.OperationFormatter(serializerCollection);

            return new Configuration(
                serializerCollection,
                parserCollection,
                formatter);
        }

        private class Configuration
        {
            public Configuration(
                ValueSerializerCollection valueSerializers,
                ResultParserCollection resultParsers,
                IOperationFormatter operationFormatter)
            {
                ValueSerializers = valueSerializers;
                ResultParsers = resultParsers;
                OperationFormatter = operationFormatter;
            }

            public ValueSerializerCollection ValueSerializers { get; }

            public ResultParserCollection ResultParsers { get; }

            public IOperationFormatter OperationFormatter { get; }
        }
    }
}
