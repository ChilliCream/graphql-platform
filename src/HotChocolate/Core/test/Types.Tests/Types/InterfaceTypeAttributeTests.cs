using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

public class InterfaceTypeAttributeTests
    : TypeTestBase
{
    [Fact]
    public void ArgumentDescriptorAttribute_Changes_DefaultValue()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInterfaceType<Interface1>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        Assert.Equal(
            "abc",
            schema.Types.GetType<InterfaceType>("Interface1")
                .Fields["field"]
                .Arguments["argument"]
                .DefaultValue!
                .Value);
    }

    [Fact]
    public void InterfaceFieldDescriptorAttribute_Adds_ContextData()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInterfaceType<Interface2>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        Assert.NotNull(
            schema.Types.GetType<InterfaceType>("Interface2")
                .Fields["field"]
                .Features.Get<CustomFeature>());
    }

    [Fact]
    public void InterfaceFieldDescriptorAttribute_Updated_FieldDefinition()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInterfaceType<Interface2>(d =>
                d.Field(t => t.GetField()).Name("foo"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        Assert.NotNull(
            schema.Types.GetType<InterfaceType>("Interface2")
                .Fields["foo"]
                .Features.Get<CustomFeature>());
    }

    [Fact]
    public void InterfaceTypeDescriptorAttribute_Add_FieldDefinition()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInterfaceType<Interface3>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        Assert.True(
            schema.Types.GetType<InterfaceType>("Interface3")
                .Fields.ContainsField("abc"));
    }

    [Fact]
    public void Annotated_Class_With_InterfaceTypeAttribute()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInterfaceType<Object1>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        Assert.True(
            schema.Types.GetType<InterfaceType>("Foo")
                .Fields.ContainsField("bar"));
    }

    public interface Interface1
    {
        string GetField([ArgumentDefaultValue("abc")] string argument);
    }

    public class ArgumentDefaultValueAttribute
        : ArgumentDescriptorAttribute
    {
        public ArgumentDefaultValueAttribute(object defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public object DefaultValue { get; }

        protected override void OnConfigure(
            IDescriptorContext context,
            IArgumentDescriptor descriptor,
            ParameterInfo parameter)
        {
            descriptor.DefaultValue(DefaultValue);
        }
    }

    public interface Interface2
    {
        [PropertyAddFeature]
        string GetField();
    }

    public class PropertyAddFeatureAttribute
        : InterfaceFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IInterfaceFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Extend().OnBeforeCompletion(
                (c, d) => d.Features.Set(new CustomFeature()));
        }
    }

    [InterfaceAddField]
    public interface Interface3
    {
        string GetField();
    }

    public class InterfaceAddFieldAttribute
        : InterfaceTypeDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IInterfaceTypeDescriptor descriptor,
            Type type)
        {
            descriptor.Field("abc").Type<StringType>();
        }
    }

    [InterfaceType(Name = "Foo")]
    public class Object1
    {
        public string? Bar { get; set; }
    }

    public sealed class CustomFeature;
}
