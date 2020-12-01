using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Http;
using Xunit;

namespace StrawberryShake.Configuration
{
    public class ClientOptionsTests
    {
        [Fact]
        public void ConfigureClient()
        {
            // arrange
            // act
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOperationClientOptions("foo")
                .AddHttpOperationPipeline(builder => builder.UseHttpDefaultPipeline())
                .AddOperationFormatter(s => new JsonOperationFormatter(s))
                .AddResultParser(s => new DummyResultParser())
                .AddValueSerializer(() => new DummyValueSerializer());

            // assert
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            IClientOptions clientOptions = services.GetRequiredService<IClientOptions>();

            Assert.IsType<JsonOperationFormatter>(
                clientOptions.GetOperationFormatter("foo"));
            Assert.IsType<DummyResultParser>(
                clientOptions.GetResultParsers("foo").Get(typeof(string)));
            Assert.IsType<DummyValueSerializer>(
                clientOptions.GetValueSerializers("foo").Get("Dummy"));
        }

        public class DummyValueSerializer : IValueSerializer
        {
            public string Name => "Dummy";

            public ValueKind Kind => throw new NotImplementedException();

            public Type ClrType => throw new NotImplementedException();

            public Type SerializationType => throw new NotImplementedException();

            public object? Deserialize(object? serialized)
            {
                throw new NotImplementedException();
            }

            public object? Serialize(object? value)
            {
                throw new NotImplementedException();
            }
        }

        public class DummyResultParser : IResultParser
        {
            public Type ResultType => typeof(string);

            public void Parse(ReadOnlySpan<byte> result, IOperationResultBuilder resultBuilder)
            {
                throw new NotImplementedException();
            }

            public Task ParseAsync(Stream stream, IOperationResultBuilder resultBuilder, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

    }
}
