using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
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



        public interface IFoo
        {
            bool Bar { get; }
            string Baz();
            int Qux(string a);
        }
    }
}
