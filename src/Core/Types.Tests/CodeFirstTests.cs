#nullable enable

using System;
using System.Threading.Tasks;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class CodeFirstTests
    {
        [Fact]
        public void InferSchemaWithNonNullRefTypes()
        {
            SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<Dog>()
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        [Fact]
        public void Type_Is_Correctly_Upgraded()
        {
            SchemaBuilder.New()
               .AddQueryType<Query>()
               .AddType<Dog>()
               .AddType<ObjectType<Dog>>()
               .AddType<DogType>()
               .Create()
               .ToString()
               .MatchSnapshot();
        }

        [Fact]
        public void Change_DefaultBinding_For_DateTime()
        {
            SchemaBuilder.New()
                .AddQueryType<QueryWithDateTimeType>()
                .BindClrType<DateTime, DateTimeType>()
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        [Fact]
        public void Remove_ClrType_Bindings_That_Are_Not_Used()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithDateTimeType>()
                .BindClrType<DateTime, DateTimeType>()
                .BindClrType<int, UrlType>()
                .ModifyOptions(o => o.RemoveUnreachableTypes = true)
                .Create();

            // assert
            bool exists = schema.TryGetType("Url", out INamedType _);
            Assert.False(exists);
        }

        public class Query
        {
            public string SayHello(string name) =>
                throw new NotImplementedException();

            public Greetings GetGreetings(Greetings greetings) =>
                throw new NotImplementedException();

            public IPet GetPet() =>
                throw new NotImplementedException();

            public Task<IPet?> GetPetOrNull() =>
                throw new NotImplementedException();
        }

        public class Greetings
        {
            public string Name { get; set; } = "Foo";
        }

        public interface IPet
        {
            string? Name { get; }
        }

        public class DogType : ObjectType<Dog>
        {
            protected override void Configure(IObjectTypeDescriptor<Dog> descriptor)
            {
                descriptor.Field("isMale").Resolver(true);
            }
        }

        public class Dog : IPet
        {
            public string? Name =>
                throw new NotImplementedException();
        }

        public class QueryWithDateTimeType : ObjectType<QueryWithDateTime>
        {
            protected override void Configure(IObjectTypeDescriptor<QueryWithDateTime> descriptor)
            {
                descriptor.Field(t => t.GetModel()).Type<ModelWithDateTimeType>();
            }
        }

        public class QueryWithDateTime
        {
            public ModelWithDateTime GetModel() => new ModelWithDateTime();
        }

        public class ModelWithDateTimeType : ObjectType<ModelWithDateTime>
        {
            protected override void Configure(IObjectTypeDescriptor<ModelWithDateTime> descriptor)
            {
                descriptor.Field(t => t.Foo).Type<DateType>();
            }
        }

        public class ModelWithDateTime
        {
            public DateTime Foo { get; set; }

            public DateTime Bar { get; set; }
        }
    }
}
