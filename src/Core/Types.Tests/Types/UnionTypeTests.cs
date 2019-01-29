using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class UnionTypeTests
    {
        [Fact]
        public void DeclareUnion_ByProvidingExplicitTypeSet()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            // act
            var fooType = new UnionType(d => d
                .Type<FooType>()
                .Type<BarType>());

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.Collection(fooType.Types.Values,
                t => Assert.Equal("Foo", t.Name),
                t => Assert.Equal("Bar", t.Name));
        }

        [Fact]
        public void DeclareUnion_InferTypeSetFromMarkerInterface()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(new FooType());
            schemaContext.Types.RegisterType(new BarType());

            // act
            var fooType = new UnionType<IFooOrBar>();

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.Collection(fooType.Types.Values,
                t => Assert.Equal("Foo", t.Name),
                t => Assert.Equal("Bar", t.Name));
        }


        [Fact]
        public void DeclareUnion_MarkerInterfaceAndTypeSet()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(new FooType());
            schemaContext.Types.RegisterType(new BarType());

            // act
            var fooType = new UnionType<IFooOrBar>(c => c.Type<BazType>());

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.Collection(fooType.Types.Values,
                t => Assert.Equal("Baz", t.Name),
                t => Assert.Equal("Foo", t.Name),
                t => Assert.Equal("Bar", t.Name));
        }

        [Fact]
        public void UnionType_AddDirectives_NameArgs()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new UnionType(d => d
                .Directive("foo")
                .Type<FooType>()
                .Type<BarType>());

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void UnionType_AddDirectives_NameArgs2()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new UnionType<SimpleInput>(d => d
                .Directive(new NameString("foo"))
                .Type<FooType>()
                .Type<BarType>());

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void UnionType_AddDirectives_DirectiveNode()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new UnionType(d => d
                .Directive(new DirectiveNode("foo"))
                .Type<FooType>()
                .Type<BarType>());

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void UnionType_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new UnionType(
                d => d.Directive(new FooDirective())
                    .Type<FooType>()
                .Type<BarType>());

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void UnionType_AddDirectives_DirectiveType()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new UnionType(
                d => d.Directive<FooDirective>()
                    .Type<FooType>()
                .Type<BarType>());

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        public class FooType
            : ObjectType<Foo>
        {
        }

        public class BarType
            : ObjectType<Bar>
        {
        }

        public class BazType
            : ObjectType<Baz>
        {
        }

        public class Foo
            : IFooOrBar
        {
            public string FooField { get; set; }
        }

        public class Bar
            : IFooOrBar
        {
            public string BarField { get; set; }
        }

        public class Baz
            : IFooOrBar
        {
            public string BazField { get; set; }
        }

        public interface IFooOrBar
        {
        }

        public class FooDirectiveType
            : DirectiveType<FooDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<FooDirective> descriptor)
            {
                descriptor.Name("foo");
                descriptor.Location(DirectiveLocation.Union)
                    .Location(DirectiveLocation.FieldDefinition);
            }
        }

        public class FooDirective { }
    }
}
