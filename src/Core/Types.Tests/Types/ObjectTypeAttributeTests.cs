using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using Moq;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Types
{
    public class ObjectTypeAttributeTests
        : TypeTestBase
    {
        [Fact]
        public void ArgumentDescriptorAttribute_Changes_DefaultValue()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Object1>()
                .Create();

            // assert
            Assert.Equal(
                "abc",
                schema.QueryType.Fields["field"].Arguments["argument"].DefaultValue.Value);
        }

        [Fact]
        public void ObjectFieldDescriptorAttribute_Adds_ContextData()
        {
            // act
            ISchema schema = SchemaBuilder.New()
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
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Object2>(d =>
                    d.Field(t => t.GetField()).Name("foo"))
                .Create();

            // assert
            Assert.Equal(
                "def",
                schema.QueryType.Fields["foo"].ContextData["abc"]);
        }

        public class Object1
        {
            public string GetField([ArgumentDefaultValue("abc")]string argument)
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

            public override void OnConfigure(IArgumentDescriptor descriptor)
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
            public override void OnConfigure(IObjectFieldDescriptor descriptor)
            {
                descriptor.Extend().OnBeforeCompletion(
                    (c, d) => d.ContextData.Add("abc", "def"));
            }
        }
    }
}
