using System;
using System.Collections.Generic;

namespace StrawberryShake.Configuration
{
    public delegate void ConfigureValueSerializers(
        IDictionary<string, IValueSerializer> serializers);

    public delegate void ConfigureResultParsers(
        IValueSerializerCollection serializers,
        IDictionary<Type, IResultParser> parsers);

    public delegate IOperationFormatter ConfigureOperationFormatter(
        IValueSerializerCollection serializers);

    public delegate void ConfigureOperationPipeline(
        IList<Delegate> pipelines);

    public class ClientOptionsModifiers
    {
        public IList<ConfigureValueSerializers> ValueSerializers { get; } =
            new List<ConfigureValueSerializers>();

        public IList<ConfigureResultParsers> ResultParsers { get; } =
            new List<ConfigureResultParsers>();

        public ConfigureOperationFormatter? OperationFormatter { get; set; }

        public IList<ConfigureOperationPipeline> OperationPipelines { get; } =
            new List<ConfigureOperationPipeline>();
    }
}
