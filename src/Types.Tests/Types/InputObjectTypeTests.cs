﻿using System.Collections.Generic;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types
{
    public class InputObjectTypeTests
    {
        [Fact]
        public void ParseLiteral()
        {
            // arrange
            Schema schema = Create();
            InputObjectType object1Type =
                schema.GetType<InputObjectType>("Object1");
            ObjectValueNode literal = CreateObjectLiteral();

            // act
            object obj = object1Type.ParseLiteral(literal);

            // assert
            Assert.IsType<SerializationInputObject1>(obj);
            obj.Snapshot();
        }

        [Fact]
        public void ParseValue()
        {
            // arrange
            Schema schema = Create();
            InputObjectType object1Type =
                schema.GetType<InputObjectType>("Object1");
            SerializationInputObject1 object1Instance =
                new SerializationInputObject1
                {
                    Foo = new SerializationInputObject2()
                };

            // act
            IValueNode value = InputObjectDefaultSerializer
                .ParseValue(object1Type, object1Instance);

            // assert
            Assert.IsType<ObjectValueNode>(value);
            value.Snapshot();
        }

        [Fact]
        public void DetectLoop()
        {
            // arrange
            Schema schema = Create();
            InputObjectType object1Type =
                schema.GetType<InputObjectType>("Object1");
            SerializationInputObject1 object1Instance =
                new SerializationInputObject1
                {
                    Foo = new SerializationInputObject2()
                };
            object1Instance.Foo.FooList.Add(object1Instance);

            // act
            IValueNode value = InputObjectDefaultSerializer
                .ParseValue(object1Type, object1Instance);

            // assert
            Assert.IsType<ObjectValueNode>(value);
            value.Snapshot();
        }

        [Fact]
        public void EnsureInputObjectTypeKindIsCorret()
        {
            // arrange
            Schema schema = Create();
            InputObjectType object1Type =
                schema.GetType<InputObjectType>("Object1");

            // act
            TypeKind kind = object1Type.Kind;

            // assert
            Assert.Equal(TypeKind.InputObject, kind);
        }

        private static ObjectValueNode CreateObjectLiteral()
        {
            return new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("foo",
                    new ObjectValueNode(new List<ObjectFieldNode>())),
                new ObjectFieldNode("bar",
                    new StringValueNode("123"))
            });
        }

        public Schema Create()
        {
            return Schema.Create(c =>
            {
                c.Options.StrictValidation = false;

                c.RegisterType(
                    new InputObjectType<SerializationInputObject1>(d =>
                    {
                        d.Name("Object1");
                        d.Field(t => t.Foo)
                            .Type<InputObjectType<SerializationInputObject2>>();
                        d.Field(t => t.Bar).Type<StringType>();
                    }));

                c.RegisterType(new InputObjectType<SerializationInputObject2>(
                    d =>
                    {
                        d.Name("Object2");
                        d.Field(t => t.FooList)
                            .Type<NonNullType<ListType<InputObjectType<
                                SerializationInputObject1>>>>();
                    }));
            });
        }
    }

    public class SerializationInputObject1
    {
        public SerializationInputObject2 Foo { get; set; }
        public string Bar { get; set; } = "Bar";
    }

    public class SerializationInputObject2
    {
        public List<SerializationInputObject1> FooList { get; set; } =
            new List<SerializationInputObject1>
        {
            new SerializationInputObject1()
        };
    }
}
