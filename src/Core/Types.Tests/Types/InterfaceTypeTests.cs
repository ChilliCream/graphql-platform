using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class InterfaceTypeTests
    {
        [Fact]
        public void InferFieldsFromClrInterface()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(new BooleanType());
            schemaContext.Types.RegisterType(new StringType());
            schemaContext.Types.RegisterType(new IntType());

            // act
            var fooType = new InterfaceType<IFoo>();
            schemaContext.Types.RegisterType(fooType);

            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);

            schemaContext.CompleteTypes();

            // assert
            Assert.Empty(errors);
            Assert.Collection(
                fooType.Fields.Where(t => !t.IsIntrospectionField),
                t =>
                {
                    Assert.Equal("bar", t.Name);
                    Assert.IsType<BooleanType>(
                        Assert.IsType<NonNullType>(t.Type).Type);
                },
                t =>
                {
                    Assert.Equal("baz", t.Name);
                    Assert.IsType<StringType>(t.Type);
                },
                t =>
                {
                    Assert.Equal("qux", t.Name);
                    Assert.IsType<IntType>(
                        Assert.IsType<NonNullType>(t.Type).Type);
                    Assert.Collection(t.Arguments,
                        a => Assert.Equal("a", a.Name));
                });
        }

        [Fact]
        public void InferSchemaInterfaceTypeFromClrInterface()
        {
            // arrange && act
            Schema schema = Schema.Create(c =>
            {
                c.RegisterType<IFoo>();
                c.RegisterType<FooImpl>();
            });

            // assert
            ObjectType type = schema.GetType<ObjectType>("FooImpl");
            Assert.Collection(type.Interfaces.Values,
                t => Assert.Equal("IFoo", t.Name));
        }

        [Fact]
        public void IgnoreFieldsFromClrInterface()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(new BooleanType());
            schemaContext.Types.RegisterType(new StringType());
            schemaContext.Types.RegisterType(new IntType());

            // act
            var fooType = new InterfaceType<IFoo>(t => t.Ignore(p => p.Bar));
            schemaContext.Types.RegisterType(fooType);

            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);

            schemaContext.CompleteTypes();

            // assert
            Assert.Empty(errors);
            Assert.Collection(
                fooType.Fields.Where(t => !t.IsIntrospectionField),
                t =>
                {
                    Assert.Equal("baz", t.Name);
                    Assert.IsType<StringType>(t.Type);
                },
                t =>
                {
                    Assert.Equal("qux", t.Name);
                    Assert.IsType<IntType>(
                        Assert.IsType<NonNullType>(t.Type).Type);
                    Assert.Collection(t.Arguments,
                        a => Assert.Equal("a", a.Name));
                });
        }

        [Fact]
        public void ExplicitInterfaceFieldDeclaration()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(new BooleanType());
            schemaContext.Types.RegisterType(new StringType());
            schemaContext.Types.RegisterType(new IntType());

            // act
            var fooType = new InterfaceType<IFoo>(t =>
                t.BindFields(BindingBehavior.Explicit)
                    .Field(p => p.Bar));
            schemaContext.Types.RegisterType(fooType);

            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);

            schemaContext.CompleteTypes();

            // assert
            Assert.Empty(errors);
            Assert.Collection(
                fooType.Fields.Where(t => !t.IsIntrospectionField),
                t =>
                {
                    Assert.Equal("bar", t.Name);
                    Assert.IsType<BooleanType>(
                        Assert.IsType<NonNullType>(t.Type).Type);
                });
        }

        [Fact]
        public void GenericInterfaceType_AddDirectives_NameArgs()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new InterfaceType<IFoo>(
                d => d.Directive("foo").Field(f => f.Bar).Directive("foo"));

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
        }

        [Fact]
        public void GenericInterfaceType_AddDirectives_NameArgs2()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new InterfaceType<IFoo>(
                d => d.Directive(new NameString("foo"))
                    .Field(f => f.Bar)
                    .Directive(new NameString("foo")));

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
        }

        [Fact]
        public void GenericInterfaceType_AddDirectives_DirectiveNode()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new InterfaceType<IFoo>(
                d => d.Directive(new DirectiveNode("foo"))
                    .Field(f => f.Bar)
                    .Directive(new DirectiveNode("foo")));

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
        }

        [Fact]
        public void GenericInterfaceType_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new InterfaceType<IFoo>(
                d => d.Directive(new FooDirective())
                    .Field(f => f.Bar)
                    .Directive(new FooDirective()));

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
        }

        [Fact]
        public void GenericInterfaceType_AddDirectives_DirectiveType()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new InterfaceType<IFoo>(
                d => d.Directive<FooDirective>()
                    .Field(f => f.Bar)
                    .Directive<FooDirective>());

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
        }

        [Fact]
        public void InterfaceType_AddDirectives_NameArgs()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new InterfaceType(
                d => d.Directive("foo")
                    .Field("id")
                    .Type<StringType>()
                    .Directive("foo"));

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InterfaceType_AddDirectives_NameArgs2()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new InterfaceType<SimpleInput>(
                d => d.Directive(new NameString("foo"))
                    .Field("id")
                    .Type<StringType>()
                    .Directive(new NameString("foo")));

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InterfaceType_AddDirectives_DirectiveNode()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new InterfaceType(
                d => d.Directive(new DirectiveNode("foo"))
                    .Field("id")
                    .Type<StringType>()
                    .Directive(new DirectiveNode("foo")));

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InterfaceType_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new InterfaceType(
                d => d.Directive(new FooDirective())
                    .Field("id")
                    .Type<StringType>()
                    .Directive(new FooDirective()));

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InterfaceType_AddDirectives_DirectiveType()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new InterfaceType(
                d => d.Directive<FooDirective>()
                    .Field("id")
                    .Type<StringType>()
                    .Directive<FooDirective>());

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        public interface IFoo
        {
            bool Bar { get; }
            string Baz();
            int Qux(string a);
        }

        public class FooImpl
            : IFoo
        {
            public bool Bar => throw new System.NotImplementedException();

            public string Baz()
            {
                throw new System.NotImplementedException();
            }

            public int Qux(string a)
            {
                throw new System.NotImplementedException();
            }
        }

        public class FooDirectiveType
            : DirectiveType<FooDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<FooDirective> descriptor)
            {
                descriptor.Name("foo");
                descriptor.Location(DirectiveLocation.Interface)
                    .Location(DirectiveLocation.FieldDefinition);
            }
        }

        public class FooDirective { }
    }
}
