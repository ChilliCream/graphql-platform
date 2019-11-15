using System;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Discovery
{
    public class SchemaTypeDiscoveryTests
    {
        [Fact]
        public void DiscoverInputArgumentTypes()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<QueryFieldArgument>();
            });

            // assert
            INamedOutputType foo =
                schema.GetType<INamedOutputType>("Foo");
            Assert.NotNull(foo);

            INamedOutputType bar =
                schema.GetType<INamedOutputType>("Bar");
            Assert.NotNull(foo);

            INamedInputType fooInput =
                schema.GetType<INamedInputType>("FooInput");
            Assert.NotNull(fooInput);

            INamedInputType barInput =
                schema.GetType<INamedInputType>("BarInput");
            Assert.NotNull(barInput);
        }

        [Fact]
        public void DiscoverOutputGraphFromMethod()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<QueryField>();
            });

            // assert
            ObjectType query = schema.GetType<ObjectType>("QueryField");
            Assert.NotNull(query);
            Assert.Collection(query.Fields.Where(t => !t.IsIntrospectionField),
                t => Assert.Equal("foo", t.Name));

            ObjectType foo = schema.GetType<ObjectType>("Foo");
            Assert.NotNull(foo);

            ObjectType bar = schema.GetType<ObjectType>("Bar");
            Assert.NotNull(foo);
        }

        [Fact]
        public void DiscoverOutputGraphFromProperty()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<QueryProperty>();
            });

            // assert
            ObjectType query = schema.GetType<ObjectType>("QueryProperty");
            Assert.NotNull(query);
            Assert.Collection(query.Fields.Where(t => !t.IsIntrospectionField),
                t => Assert.Equal("foo", t.Name));

            ObjectType foo = schema.GetType<ObjectType>("Foo");
            Assert.NotNull(foo);

            ObjectType bar = schema.GetType<ObjectType>("Bar");
            Assert.NotNull(foo);
        }

        [Fact]
        public void DiscoverOutputGraphAndIgnoreVoidMethods()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<QueryMethodVoid>();
            });

            // assert
            ObjectType query = schema.GetType<ObjectType>("QueryMethodVoid");
            Assert.NotNull(query);
            Assert.Collection(query.Fields.Where(t => !t.IsIntrospectionField),
                t => Assert.Equal("foo", t.Name));

            ObjectType foo = schema.GetType<ObjectType>("Foo");
            Assert.NotNull(foo);

            ObjectType bar = schema.GetType<ObjectType>("Bar");
            Assert.NotNull(foo);
        }

        [Fact]
        public void InferEnumAsEnumType()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterType<FooBar>();
                c.Options.StrictValidation = false;
            });

            // assert
            EnumType fooBar = schema.GetType<EnumType>("FooBar");
            Assert.NotNull(fooBar);
            Assert.Collection(fooBar.Values,
                t => Assert.Equal("FOO", t.Name),
                t => Assert.Equal("BAR", t.Name));
        }

        [Fact]
        public void InferCustomScalarTypes()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithCustomScalar>()
                .AddType<ByteArrayType>()
                .Create();

            // assert
            ObjectType fooByte = schema.GetType<ObjectType>("FooByte");
            Assert.NotNull(fooByte);

            ObjectField field = fooByte.Fields["bar"];
            Assert.Equal("ByteArray", field.Type.NamedType().Name);
        }

        public class QueryFieldArgument
        {
            public Bar Bar { get; }

            public Foo GetFoo(Foo foo)
            {
                return foo;
            }
        }

        public class QueryField
        {
            public Foo GetFoo()
            {
                return null;
            }
        }

        public class QueryProperty
        {
            public Foo Foo { get; }
        }

        public class QueryMethodVoid
        {
            public Foo GetFoo()
            {
                return null;
            }

            public void GetBar()
            {
            }
        }

        public class QueryWithCustomScalar
        {
            public FooByte GetFoo(FooByte foo)
            {
                return null;
            }
        }

        public class Foo
        {
            public Bar Bar { get; set; }
        }

        public class FooByte
        {
            public byte[] Bar { get; set; }
        }

        public class Bar
        {
            public string Baz { get; set; }
        }

        public enum FooBar
        {
            Foo,
            Bar
        }

        public class ByteArrayType
            : ScalarType
        {
            public ByteArrayType()
                : base("ByteArray")
            {
            }

            public override Type ClrType => typeof(byte[]);

            public override bool IsInstanceOfType(IValueNode literal)
            {
                throw new NotSupportedException();
            }

            public override object ParseLiteral(IValueNode literal)
            {
                throw new NotSupportedException();
            }

            public override IValueNode ParseValue(object value)
            {
                throw new NotSupportedException();
            }

            public override object Serialize(object value)
            {
                throw new NotSupportedException();
            }

            public override object Deserialize(object value)
            {
                throw new NotSupportedException();
            }

            public override bool TryDeserialize(
                object serialized, out object value)
            {
                throw new NotSupportedException();
            }

            public override bool TrySerialize(
                object value, out object serialized)
            {
                throw new NotSupportedException();
            }
        }
    }
}
