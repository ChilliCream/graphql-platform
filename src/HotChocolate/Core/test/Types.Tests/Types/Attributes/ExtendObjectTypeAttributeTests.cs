namespace HotChocolate.Types;

public class ExtendObjectTypeAttributeTests
{
    [Fact]
    public void NonGeneric_ImplicitlyExtends()
    {
        SchemaBuilder.New()
            .AddQueryType<FooType>()
            .AddType<NonGenericExtendFoo>()
            .ModifyOptions(options => options.DefaultBindingBehavior = BindingBehavior.Explicit)
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public void Generic_ImplicitlyExtends()
    {
        SchemaBuilder.New()
            .AddQueryType<FooType>()
            .AddType<GenericExtendFoo>()
            .ModifyOptions(options => options.DefaultBindingBehavior = BindingBehavior.Explicit)
            .Create()
            .Print()
            .MatchSnapshot();
    }

    public class Foo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IgnoreMe { get; set; }
    }

    public class FooType : ObjectType<Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(x => x.Id);
            descriptor.Field(x => x.Name);
        }
    }

    [ExtendObjectType(typeof(Foo))]
    public class NonGenericExtendFoo
    {
        public string GetBar() => "bar";
    }

    [ExtendObjectType<Foo>]
    public class GenericExtendFoo
    {
        public string GetBar() => "bar";
    }
}
