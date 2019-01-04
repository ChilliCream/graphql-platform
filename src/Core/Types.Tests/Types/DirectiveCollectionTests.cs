using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using Moq;
using Xunit;

namespace HotChocolate.Types
{
    public class DirectiveCollectionTests
    {
        [Fact]
        public void DirectiveOrderIsSignificant()
        {
            // act
            var someType = new ObjectType(t => t.Name("Foo"));
            var directiveDescriptions = new List<DirectiveDescription>
            {
                new DirectiveDescription(new DirectiveNode("foo")),
                new DirectiveDescription(new DirectiveNode("bar"))
            };

            var foo = new DirectiveType(d =>
                d.Name("foo").Location(DirectiveLocation.Field));

            var bar = new DirectiveType(d =>
                d.Name("bar").Location(DirectiveLocation.Field));

            var context = new Mock<ITypeInitializationContext>(
                MockBehavior.Strict);
            context.Setup(
                t => t.GetDirectiveType(It.IsAny<DirectiveReference>()))
                .Returns(new Func<DirectiveReference, DirectiveType>(
                    r =>
                    {
                        if (r.Name == "foo")
                        {
                            return foo;
                        }

                        if (r.Name == "bar")
                        {
                            return bar;
                        }

                        return null;
                    }
                ));


            // act
            var collection = new DirectiveCollection(
                someType, DirectiveLocation.Field,
                directiveDescriptions);
            ((INeedsInitialization)collection)
                .RegisterDependencies(context.Object);
            ((INeedsInitialization)collection)
                .CompleteType(context.Object);

            // assert
            Assert.Collection(collection,
                t => Assert.Equal("foo", t.Name),
                t => Assert.Equal("bar", t.Name));
        }

        [Fact]
        public void DirectiveNotRepeatable()
        {
            // act
            var someType = new ObjectType(t => t.Name("Foo"));
            var directiveDescriptions = new List<DirectiveDescription>
            {
                new DirectiveDescription(new DirectiveNode("foo")),
                new DirectiveDescription(new DirectiveNode("foo"))
            };

            var foo = new DirectiveType(d =>
                d.Name("foo").Location(DirectiveLocation.Field));

            var errors = new List<SchemaError>();

            var context = new Mock<ITypeInitializationContext>();
            context.Setup(
                t => t.GetDirectiveType(It.IsAny<DirectiveReference>()))
                .Returns(new Func<DirectiveReference, DirectiveType>(
                    r => foo));
            context.Setup(t => t.ReportError(It.IsAny<SchemaError>()))
                .Callback(new Action<SchemaError>(errors.Add));

            // act
            var collection = new DirectiveCollection(
                someType, DirectiveLocation.Field,
                directiveDescriptions);
            ((INeedsInitialization)collection)
                .RegisterDependencies(context.Object);
            ((INeedsInitialization)collection)
                .CompleteType(context.Object);

            // assert
            Assert.Collection(errors,
                t => Assert.Equal(
                    "The specified directive `@foo` " +
                    "is unique and cannot be added twice.",
                    t.Message));
        }

        [Fact]
        public void InvalidLocation()
        {
            // act
            var someType = new ObjectType(t => t.Name("Foo"));
            var directiveDescriptions = new List<DirectiveDescription>
            {
                new DirectiveDescription(new DirectiveNode("foo"))
            };

            var foo = new DirectiveType(d =>
                d.Name("foo").Location(DirectiveLocation.Enum));

            var errors = new List<SchemaError>();

            var context = new Mock<ITypeInitializationContext>();
            context.Setup(
                t => t.GetDirectiveType(It.IsAny<DirectiveReference>()))
                .Returns(new Func<DirectiveReference, DirectiveType>(
                    r => foo));
            context.Setup(t => t.ReportError(It.IsAny<SchemaError>()))
                .Callback(new Action<SchemaError>(errors.Add));

            // act
            var collection = new DirectiveCollection(
                someType, DirectiveLocation.Field,
                directiveDescriptions);
            ((INeedsInitialization)collection)
                .RegisterDependencies(context.Object);
            ((INeedsInitialization)collection)
                .CompleteType(context.Object);

            // assert
            Assert.Collection(errors,
                t => Assert.Equal(
                    "The specified directive `@foo` " +
                    "is not allowed on the current location " +
                    $"`{DirectiveLocation.Field}`.",
                    t.Message));
        }
    }
}
