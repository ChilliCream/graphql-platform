
using System;
using Prometheus.Abstractions;
using Xunit;

namespace Prometheus.Execution.SelectionResultProcessors
{
    public class SelectionResultProcessorResolverTests
    {
        [Fact]
        public void ScalarListType()
        {
            // arrange
            ISchemaDocument schema = CreateSchema();
            ListType type = new ListType(NamedType.String);

            // act
            ISelectionResultProcessor processor = SelectionResultProcessorResolver.GetProcessor(schema, type);

            // assert
            Assert.Equal(ScalarListSelectionResultProcessor.Default, processor);
        }

        [Fact]
        public void ObjectListType()
        {
            // arrange
            ISchemaDocument schema = CreateSchema();
            ListType type = new ListType(new NamedType("MyObject"));

            // act
            ISelectionResultProcessor processor = SelectionResultProcessorResolver.GetProcessor(schema, type);

            // assert
            Assert.Equal(ObjectListSelectionResultProcessor.Default, processor);
        }

        [Fact]
        public void ScalarType()
        {
            // arrange
            ISchemaDocument schema = CreateSchema();
            NamedType type = NamedType.Boolean;

            // act
            ISelectionResultProcessor processor = SelectionResultProcessorResolver.GetProcessor(schema, type);

            // assert
            Assert.Equal(ScalarSelectionResultProcessor.Default, processor);
        }

        [Fact]
        public void EnumType()
        {
            // arrange
            ISchemaDocument schema = CreateSchema();
            NamedType type = new NamedType("MyEnum");

            // act
            ISelectionResultProcessor processor = SelectionResultProcessorResolver.GetProcessor(schema, type);

            // assert
            Assert.Equal(ScalarSelectionResultProcessor.Default, processor);
        }

        [Fact]
        public void NonNullEnumType()
        {
            // arrange
            ISchemaDocument schema = CreateSchema();
            NonNullType type = new NonNullType(new NamedType("MyEnum"));

            // act
            ISelectionResultProcessor processor = SelectionResultProcessorResolver.GetProcessor(schema, type);

            // assert
            Assert.Equal(ScalarSelectionResultProcessor.Default, processor);
        }

        [Fact]
        public void ObjectType()
        {
            // arrange
            ISchemaDocument schema = CreateSchema();
            NamedType type = new NamedType("MyObject");

            // act
            ISelectionResultProcessor processor = SelectionResultProcessorResolver.GetProcessor(schema, type);

            // assert
            Assert.Equal(ObjectSelectionResultProcessor.Default, processor);
        }

        [Fact]
        public void TypeIsNull()
        {
            // arrange
            ISchemaDocument schema = CreateSchema();

            // act
            Action a = () => SelectionResultProcessorResolver.GetProcessor(schema, null);

            // assert
            Assert.Throws<ArgumentNullException>("fieldType", a);
        }

        private static ISchemaDocument CreateSchema()
        {
            EnumTypeDefinition enumType =
                new EnumTypeDefinition("MyEnum", new[] { "A", "B" });
            return new SchemaDocument(new[] { enumType });
        }
    }
}