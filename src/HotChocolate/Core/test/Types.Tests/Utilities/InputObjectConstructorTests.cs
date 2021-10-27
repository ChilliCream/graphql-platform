using System;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Utilities
{
    public class InputObjectConstructorTests
    {
        [Theory]
        [InlineData(typeof(Mutation1))]
        [InlineData(typeof(Mutation2))]
        [InlineData(typeof(Mutation3))]
        [InlineData(typeof(Mutation4))]
        [InlineData(typeof(Mutation5))]
        [InlineData(typeof(Mutation6))]
        public void InputObjectConstructorValidation_Works(Type mutationType)
        {
            var nameExt = new Snapshooter.SnapshotNameExtension(mutationType.Name);

            // arrange
            // act
            ISchema schema;
            try
            {
                schema = SchemaBuilder.New()
                    .AddQueryType<Query>()
                    .AddMutationType(mutationType)
                    .Create();
            }
            catch (Exception ex)
            {
                // assert
                ex.Message.MatchSnapshot(nameExt);
                return;
                // TODO: Make the error message clearer when you've got random other args that shouldn't be there
            }

            schema.ToString().MatchSnapshot(nameExt);

            // TODO: Run a query with the input and make sure the value is set and returned
            // Mostly interested to see if a property with a public setter when there's a non-default ctor is set
        }

        public class Query
        {
            public string Lol => "";
        }

        public class Mutation1
        {
            public int CreateFoo1(CreateFoo1Input input) => input.FooId;
        }

        public class CreateFoo1Input
        {
            public int FooId { get; }

            public CreateFoo1Input()
            {
                // Should fail and mentioned missing arg
            }
        }

        public class Mutation2
        {
            public int CreateFoo2(CreateFoo2Input input) => input.FooId;
        }

        public class CreateFoo2Input
        {
            public int FooId { get; }

            public CreateFoo2Input(int wrongId)
            {
                // Should fail and mentioned missing arg
                FooId = wrongId;
            }
        }

        public class Mutation3
        {
            public int CreateFoo3(CreateFoo3Input input) => input.FooId;
        }

        public class CreateFoo3Input
        {
            public int FooId { get; set; }

            public CreateFoo3Input()
            {
                // Should work, Foo id has public setter
            }
        }

        public class Mutation4
        {
            public int CreateFoo4(CreateFoo4Input input) => input.FooId;
        }

        public class CreateFoo4Input
        {
            public int FooId { get; }

            public CreateFoo4Input(int fooId)
            {
                // Should work, Foo id has public setter
                FooId = fooId;
            }
        }

        public class Mutation5
        {
            public int CreateFoo5(CreateFoo5Input input) => input.FooId;
        }

        public class CreateFoo5Input
        {
            public int FooId { get; }

            public CreateFoo5Input(int fooId, int randomOtherThing)
            {
                // Should fail, random arg in ctor
                FooId = fooId;
            }
        }

        public class Mutation6
        {
            public int CreateFoo6(CreateFoo6Input input) => input.FooId;
        }

        public class CreateFoo6Input
        {
            public int FooId { get; }

            public CreateFoo6Input(string fooId)
            {
                // Should fail, arg is of wrong type
                //FooId = FooId;
            }
        }

        public class Mutation7
        {
            public int CreateFoo7(CreateFoo7Input input) => input.FooId;
        }

        public class CreateFoo7Input
        {
            public int FooId { get; set; }

            public CreateFoo7Input(int randomOtherThing)
            {
                // Should fail, random arg in ctor
            }
        }
    }
}
