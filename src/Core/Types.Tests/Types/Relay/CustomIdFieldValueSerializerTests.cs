using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class CustomIdFieldValueSerializerTests
    {
        private static ISchema GetSchema()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIdFieldValueSerializerFactory, PolymorphicIdFieldValueSerializerFactory>();

            return SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<FooPayload>()
                .AddServices(services.BuildServiceProvider())
                .Create();
        }

        private static async Task<IExecutionResult> GetExecutionResultAsync(string query)
        {
            return await GetSchema()
                .MakeExecutable()
                .ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(query)
                        .Create());
        }

        [Fact]
        public void Schema_Is_As_Expected()
        {
            // arrange / act
            GetSchema().ToString().MatchSnapshot();
        }

        [Theory]
        [InlineData("intId", "1")]
        [InlineData("intId", "\"1\"")]
        [InlineData("longId", "9223372036854775807")]
        [InlineData("longId", "\"9223372036854775807\"")]
        [InlineData("guidId", "\"3ba284b6-2a57-4744-80a7-21e26ece92a2\"")]
        public async Task PolymorphicIdFieldValueSerializer_CoercesIds(string field, string idArg)
        {
            // arrange
            var query = "query { " + field + "(id: " + idArg + ") }";

            // act
            IExecutionResult result = await GetExecutionResultAsync(query);

            // assert
            new
            {
                result = result.ToJson(),
                idArg
            }.MatchSnapshot(new SnapshotNameExtension(
                "Field_" + field,
                "IdArg_" + idArg.Replace("\"", "-")));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PolymorphicIdFieldValueSerializer_HandleStringIds(bool useGlobalId)
        {
            // arrange
            var idArg = useGlobalId
                ? new IdSerializer().Serialize("SomeTypeWithStringId", "1234")
                : "1234";

            var query = "query { stringId(id: \"" + idArg + "\") }";

            // act
            IExecutionResult result = await GetExecutionResultAsync(query);

            // assert
            new
            {
                result = result.ToJson(),
                idArg
            }.MatchSnapshot(new SnapshotNameExtension(
                "UseGlobalId" + useGlobalId.ToString()));
        }

        public class PolymorphicIdFieldValueSerializerFactory : IIdFieldValueSerializerFactory
        {
            public IdFieldValueSerializer Create(
                NameString typeName,
                IIdSerializer innerSerializer,
                bool validateType,
                bool isListType,
                Type valueType)
            {
                return new PolymorphicIdFieldValueSerializer(
                      typeName,
                      innerSerializer,
                      validateType,
                      isListType,
                      valueType);
            }
        }

        public class PolymorphicIdFieldValueSerializer : IdFieldValueSerializer
        {
            public PolymorphicIdFieldValueSerializer(
                NameString typeName,
                IIdSerializer innerSerializer,
                bool validateType,
                bool isListType,
                Type valueType)
                : base(
                      typeName,
                      innerSerializer,
                      validateType,
                      isListType,
                      valueType)
            {

            }

            protected override IdValue DeserializeId(string value)
            {
                try
                {
                    return base.DeserializeId(value);
                }
                catch
                {
                    // Allow to fall through as this is likely a non-serialized id
                }

                if (ValueType == typeof(int) &&
                    value is string rawIntString && int.TryParse(rawIntString, out var intValue))
                {
                    return new IdValue(SchemaName, TypeName, intValue);
                }

                if (ValueType == typeof(long) &&
                    value is string rawLongString && long.TryParse(rawLongString, out var longValue))
                {
                    return new IdValue(SchemaName, TypeName, longValue);
                }

                if (ValueType == typeof(Guid) &&
                    value is string rawGuidString && Guid.TryParse(rawGuidString, out Guid guidValue))
                {
                    return new IdValue(SchemaName, TypeName, guidValue);
                }

                if (ValueType == typeof(string))
                {
                    // Note: there's a chance that value could be an invalid base64-encoded string
                    // that was incorrectly passed, but it's difficult to know so we let it slide

                    return new IdValue(SchemaName, TypeName, value);
                }

                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage("The specified value is not a valid ID value.")
                        .Build());
            }
        }

        public class Query
        {
            public int IntId([ID("SomeTypeWithIntId")] int id) => id;

            public long LongId([ID("SomeTypeWithLongId")] long id) => id;

            public Guid GuidId([ID("SomeTypeWithGuidId")] Guid id) => id;

            public string StringId([ID("SomeTypeWithStringId")] string id) => id;

            public FooPayload Foo(FooInput input) => new FooPayload
            {
                IntId = input.IntId,
                LongId = input.LongId,
                GuidId = input.GuidId,
                StringId = input.StringId
            };
        }

        public class FooInput : IIdFieldVariants
        {
            [ID("SomeTypeWithIntId")]
            public int IntId { get; set; }

            [ID("SomeTypeWithLongId")]
            public long LongId { get; set; }

            [ID("SomeTypeWithGuidId")]
            public Guid GuidId { get; set; }

            [ID("SomeTypeWithStringId")]
            public string StringId { get; set; }
        }

        public class FooPayload : IIdFieldVariants
        {
            public int IntId { get; set; }

            public long LongId { get; set; }

            public Guid GuidId { get; set; }

            public string StringId { get; set; }
        }

        public interface IIdFieldVariants
        {
            int IntId { get; set; }

            long LongId { get; set; }

            Guid GuidId { get; set; }

            string StringId { get; set; }
        }
    }
}
