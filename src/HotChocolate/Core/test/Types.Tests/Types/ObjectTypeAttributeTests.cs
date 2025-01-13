using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace HotChocolate.Types;

public class ObjectTypeAttributeTests
    : TypeTestBase
{
    [Fact]
    public void ArgumentDescriptorAttribute_Changes_DefaultValue()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Object1>()
            .Create();

        // assert
        Assert.Equal(
            "abc",
            schema.QueryType.Fields["field"].Arguments["argument"].DefaultValue!.Value);
    }

    [Fact]
    public void ObjectFieldDescriptorAttribute_Adds_ContextData()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Object2>()
            .Create();

        // assert
        Assert.Equal(
            "def",
            schema.QueryType.Fields["field"].ContextData["abc"]);
    }

    [Fact]
    public void ObjectFieldDescriptorAttribute_Updated_FieldDefinition()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Object2>(d =>
                d.Field<string>(t => t.GetField()).Name("foo"))
            .Create();

        // assert
        Assert.Equal(
            "def",
            schema.QueryType.Fields["foo"].ContextData["abc"]);
    }

    [Fact]
    public void ObjectTypeDescriptorAttribute_Add_FieldDefinition()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Object3>()
            .Create();

        // assert
        Assert.True(schema.QueryType.Fields.ContainsField("abc"));
    }

    [Fact]
    public void ObjectTypeDescriptorAttribute_Add_FieldDefinition_2()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddObjectType<Object3>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        Assert.True(schema.GetType<ObjectType>("Object3").Fields.ContainsField("abc"));
    }

    [Fact]
    public void ObjectTypeAttribute_Mark_Struct_As_ObjectType()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddType<StructQuery>()
            .ModifyOptions(o => o.RemoveUnreachableTypes = true)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void ExtendObjectTypeAttribute_Extend_Query_Type()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddType<StructQuery>()
            .AddType<StructQueryExtension>()
            .ModifyOptions(o => o.RemoveUnreachableTypes = true)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task ExtendObjectTypeAttribute_Extend_Query_Type_2()
    {
        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddType<StructQuery>()
                .AddType<StructQueryExtension>()
                .TrimTypes()
                .BuildSchemaAsync();

        // assert
        schema.ToString().MatchSnapshot();
    }

    public class Object1
    {
        public string GetField([ArgumentDefaultValue("abc")] string argument)
        {
            throw new NotImplementedException();
        }
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
            ParameterInfo parameterInfo)
        {
            descriptor.DefaultValue(DefaultValue);
        }
    }

    public class Object2
    {
        [PropertyAddContextData]
        public string GetField()
        {
            throw new NotImplementedException();
        }
    }

    public class PropertyAddContextDataAttribute
        : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Extend().OnBeforeCompletion(
                (c, d) => d.ContextData.Add("abc", "def"));
        }
    }

    [ObjectAddField]
    public class Object3
    {
        public string GetField()
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectAddFieldAttribute
        : ObjectTypeDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type type)
        {
            descriptor.Field("abc").Resolve<string>("def");
        }
    }

    [ObjectType("Query")]
    public struct StructQuery
    {
        public string? Foo { get; }
    }

    [ExtendObjectType("Query")]
    public class StructQueryExtension
    {
        public string? Bar { get; }
    }
}
