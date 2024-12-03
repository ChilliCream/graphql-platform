using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

public class UnionTypeAttributeTests
{
    [Fact]
    public void SetName_Union_Interface()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddUnionType<IUnion1>()
            .AddType<Foo>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        Assert.NotNull(schema.GetType<UnionType>("Abc"));
    }

    [Fact]
    public void UnionTypeAttribute_Infer_Union()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddType<IUnion2>()
            .AddType<Union2Type1>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [SetName]
    public interface IUnion1;

    public class Foo : IUnion1;

    public class SetNameAttribute : UnionTypeDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IUnionTypeDescriptor descriptor,
            Type type)
        {
            descriptor.Name("Abc");
        }
    }

    [UnionType(Name = "Union")]
    public interface IUnion2;

    [ObjectType(Name = "Type")]
    public class Union2Type1 : IUnion2;
}
