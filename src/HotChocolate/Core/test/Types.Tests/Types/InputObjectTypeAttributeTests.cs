using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Tests;
using HotChocolate.Types.Descriptors;
using Snapshooter.Xunit;
using Xunit;
using QueryRequestBuilder = HotChocolate.Execution.QueryRequestBuilder;

namespace HotChocolate.Types
{
    public class InputObjectTypeAttributeTests
    {
        [Fact]
        public void Change_Field_Name_With_Attribute()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Object1>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.True(
                schema.GetType<InputObjectType>("Object1Input")
                    .Fields
                    .ContainsField("bar"));
        }

        [Fact]
        public void Change_InputObjectType_Name_With_Attribute()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Object2>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.True(
                schema.GetType<InputObjectType>("Bar")
                    .Fields
                    .ContainsField("foo"));
        }

        [Fact]
        public void Annotated_Struct1_With_InputObjectTypeAttribute()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Struct1>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.True(
                schema.GetType<InputObjectType>("Foo")
                    .Fields
                    .ContainsField("foo"));
        }

        [Fact]
        public void Infer_Default_Values_From_Attribute()
        {
            SchemaBuilder.New()
                .AddInputObjectType<InputWithDefaults>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public async Task Infer_Default_Values_From_Attribute_Execute()
        {
            await SchemaBuilder.New()
                .AddQueryType(d =>
                {
                    d.Name("Query");

                    d.Field("foo")
                        .Argument("a", a => a.Type<InputObjectType<InputWithDefaults>>())
                        .Resolve(ctx => ctx.ArgumentValue<InputWithDefaults>("a"));
                })
                .AddInputObjectType<InputWithDefaults>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create()
                .MakeExecutable()
                .ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery("{ foo(a: { }) { foo bar baz qux quux } }")
                        .Create())
                .MatchSnapshotAsync();
        }


        [Fact]
        public async Task Infer_Default_Values_From_Attribute_Execute_With_Variables()
        {
            await SchemaBuilder.New()
                .AddQueryType(d =>
                {
                    d.Name("Query");

                    d.Field("foo")
                        .Argument("a", a => a.Type<InputObjectType<InputWithDefaults>>())
                        .Resolve(ctx => ctx.ArgumentValue<InputWithDefaults>("a"));
                })
                .AddInputObjectType<InputWithDefaults>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create()
                .MakeExecutable()
                .ExecuteAsync(
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                            query($q: InputWithDefaultsInput) {
                                foo(a: $q) {
                                    foo bar baz qux quux
                                }
                            }")
                        .SetVariableValue("q", new Dictionary<string, object>())
                        .Create())
                .MatchSnapshotAsync();
        }

        public class Object1
        {
            [RenameField]
            public string Foo { get; set; }
        }

        public class RenameFieldAttribute
            : InputFieldDescriptorAttribute
        {
            public override void OnConfigure(
                IDescriptorContext context,
                IInputFieldDescriptor descriptor,
                MemberInfo member)
            {
                descriptor.Name("bar");
            }
        }

        [RenameType]
        public class Object2
        {
            public string Foo { get; set; }
        }

        [InputObjectType(Name = "Foo")]
        public struct Struct1
        {
            public string Foo { get; set; }
        }

        public class RenameTypeAttribute
            : InputObjectTypeDescriptorAttribute
        {
            public override void OnConfigure(
                IDescriptorContext context,
                IInputObjectTypeDescriptor descriptor,
                Type type)
            {
                descriptor.Name("Bar");
            }
        }

        public class InputWithDefaults
        {
            [DefaultValue("DefaultValue123")]
            public string Foo { get; set; }

            [DefaultValue(2)]
            public int Bar { get; set; }

            [DefaultValue(1.2)]
            public double Baz { get; set; }

            [DefaultValue(true)]
            public bool Qux { get; set; }

            [DefaultValue(Quux.Corge)]
            public Quux Quux { get; set; }
        }

        public enum Quux
        {
            Corge,
            Grault
        }
    }
}
