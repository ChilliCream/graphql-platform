using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration;

public class TypeInitializationOrderTests
{
    [Fact]
    public void Merge_Type_Extensions_AB_KeepOrder()
    {
        // the field order will change depending on the extension order.
        SchemaBuilder.New()
            .AddQueryType(c => c.Name("Query"))
            .AddType<QueryExtensionType_A>()
            .AddType<QueryExtensionType_B>()
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public void Merge_Type_Extensions_BA_KeepOrder()
    {
        SchemaBuilder.New()
            .AddQueryType(c => c.Name("Query"))
            .AddType<QueryExtensionType_B>()
            .AddType<QueryExtensionType_A>()
            .Create()
            .Print()
            .MatchSnapshot(postFix: "BA");
    }

    public class QueryExtensionType_A : ObjectTypeExtension
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query")
                .Field("words")
                .Type<ListType<ObjectType<Word>>>()
                .Resolve(
                    new Word[] { new() { Value = "Hello", }, new() { Value = "World", }, })
                .Extend()
                .OnBeforeCreate((c,d) =>
                {
                    var reference =
                        c.TypeInspector.GetTypeRef(typeof(Word), TypeContext.Output);

                    d.Configurations.Add(
                        new CompleteConfiguration<ObjectFieldDefinition>(
                            (context, _) =>
                            {
                                var type = context.GetType<ObjectType>(reference);

                                if (!type.IsCompleted)
                                {
                                    throw new Exception("Order should not matter");
                                }
                            },
                            d,
                            ApplyConfigurationOn.BeforeCompletion,
                            reference,
                            TypeDependencyFulfilled.Completed));
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
                .Resolve("Hello world!");
        }
    }

    public class Word
    {
        public string Value { get; set; }
    }
}
