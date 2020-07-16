using System;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Configuration
{
    public class TypeInitializationOrderTests
    {
        [Fact]
        public void TypeDependencies_Are_Correctly_Copied_From_Extension_To_Type()
        {
            // the order should not make any difference (AB BA)
            SchemaBuilder.New()
                .AddQueryType(c => c.Name("Query"))
                .AddType<QueryExtensionType_A>()
                .AddType<QueryExtensionType_B>()
            .Create()
            .ToString()
            .MatchSnapshot();

            SchemaBuilder.New()
                .AddQueryType(c => c.Name("Query"))
                .AddType<QueryExtensionType_B>()
                .AddType<QueryExtensionType_A>()
            .Create()
            .ToString()
            .MatchSnapshot();
        }

        public class QueryExtensionType_A : ObjectTypeExtension
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query")
                    .Field("words")
                    .Type<ListType<ObjectType<Word>>>()
                    .Resolver(
                        new Word[] { new Word { Value = "Hello" }, new Word { Value = "World" } })
                    .Extend()
                    .OnBeforeCreate(d =>
                    {
                        var reference = TypeReference.Create(typeof(Word), TypeContext.Output);

                        ILazyTypeConfiguration lazyConfiguration =
                            LazyTypeConfigurationBuilder
                                .New<ObjectFieldDefinition>()
                                .Definition(d)
                                .Configure((context, definition) =>
                                {
                                    ObjectType type = context.GetType<ObjectType>(reference);
                                    if (!type.IsCompleted)
                                    {
                                        throw new Exception("Order should not matter");
                                    }
                                })
                                .On(ApplyConfigurationOn.Completion)
                                .DependsOn(reference, true)
                                .Build();

                        d.Configurations.Add(lazyConfiguration);
                    });
            }
        }

        public class QueryExtensionType_B : ObjectTypeExtension
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query")
                    .Field("greeting")
                    .Type<StringType>()
                    .Resolver("Hello world!");
            }
        }

        public class Word
        {
            public string Value { get; set; }
        }
    }
}
