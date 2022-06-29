using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Tests;

public class FilterInputTypeTest : FilterTestBase
{
    [Fact]
    public void FilterInputType_DynamicName()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddType(
                new FilterInputType<Foo>(
                    d => d
                        .Name(dep => dep.Name + "Foo")
                        .DependsOn<StringType>()
                        .Field(x => x.Bar))));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_DynamicName_NonGeneric()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddType(
                new FilterInputType<Foo>(
                    d => d.Name(dep => dep.Name + "Foo")
                        .DependsOn(typeof(StringType))
                        .Field(x => x.Bar))));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInput_AddDirectives_NameArgs()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddDirectiveType<FooDirectiveType>()
                .AddType(
                    new FilterInputType<Foo>(
                        d => d.Directive("foo")
                            .Field(x => x.Bar))));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInput_AddDirectives_NameArgs2()
    {
        // arrange
        // act
        var schema = CreateSchema(s => s
            .AddDirectiveType<FooDirectiveType>()
            .AddType(new FilterInputType<Foo>(d => d.Directive("foo").Field(x => x.Bar))));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInput_AddDirectives_DirectiveNode()
    {
        // arrange
        // act
        var schema = CreateSchema(s => s
            .AddDirectiveType<FooDirectiveType>()
            .AddType(new FilterInputType<Foo>(d => d
                .Directive(new DirectiveNode("foo")).Field(x => x.Bar))));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInput_AddDirectives_DirectiveClassInstance()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddDirectiveType<FooDirectiveType>()
                .AddType(
                    new FilterInputType<Foo>(
                        d => d
                            .Directive(new FooDirective())
                            .Field(x => x.Bar))));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInput_AddDirectives_DirectiveType()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddDirectiveType<FooDirectiveType>()
                .AddType(
                    new FilterInputType<Foo>(
                        d => d
                            .Directive<FooDirective>()
                            .Field(x => x.Bar))));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInput_AddDescription()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddType(
                new FilterInputType<Foo>(
                    d => d.Description("Test").Field(x => x.Bar))));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInput_AddName()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddType(
                new FilterInputType<Foo>(
                    d => d.Name("Test").Field(x => x.Bar))));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_ImplicitBinding()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .ModifyOptions(x => x.DefaultBindingBehavior = BindingBehavior.Explicit)
            .AddFiltering()
            .AddType(new ObjectType<Foo>(x => x.Field(x => x.Bar)))
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<ObjectType<Foo>>()
                        .Resolve("bar")
                        .UseFiltering<Foo>(x => x.BindFieldsImplicitly()))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_ImplicitBinding_BindFields()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .ModifyOptions(x => x.DefaultBindingBehavior = BindingBehavior.Explicit)
            .AddFiltering()
            .AddType(new ObjectType<Foo>(x => x.Field(x => x.Bar)))
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<ObjectType<Foo>>()
                        .Resolve("bar")
                        .UseFiltering<Foo>(x => x.BindFields(BindingBehavior.Implicit)))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_ExplicitBinding()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .ModifyOptions(x => x.DefaultBindingBehavior = BindingBehavior.Implicit)
            .AddFiltering()
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<ObjectType<Bar>>()
                        .Resolve("bar")
                        .UseFiltering<Bar>(x => x.BindFieldsExplicitly().Field(y => y.Qux)))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_ExplicitBinding_BindFields()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .ModifyOptions(x => x.DefaultBindingBehavior = BindingBehavior.Implicit)
            .AddFiltering()
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<ObjectType<Bar>>()
                        .Resolve("bar")
                        .UseFiltering<Bar>(
                            x => x.BindFields(BindingBehavior.Explicit).Field(y => y.Qux)))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Should_ThrowException_WhenNoConventionIsRegistered()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Resolve(new List<Foo>())
                        .UseFiltering("Foo"));

        // act
        // assert
        var exception = Assert.Throws<SchemaException>(() => builder.Create());
        exception.Message.MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Should_ThrowException_WhenNoConventionIsRegisteredDefault()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Resolve(new List<Foo>())
                        .UseFiltering());

        // act
        // assert
        var exception = Assert.Throws<SchemaException>(() => builder.Create());
        exception.Message.MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Should_UseCustomFilterInput_When_Nested()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType<UserQueryType>();

        // act
        // assert
        builder.Create().Print().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Should_NotOverrideHandler_OnBeforeCreate()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType<CustomHandlerQueryType>()
            .Create();

        // act
        builder.TryGetType<CustomHandlerFilterInputType>(
            "TestName",
            out var type);

        // assert
        Assert.NotNull(type);
        Assert.IsType<CustomHandler>(Assert.IsType<FilterField>(type!.Fields["id"]).Handler);
    }

    [Fact]
    public void FilterInputType_Should_NotOverrideHandler_OnBeforeCompletion()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType<CustomHandlerQueryType>()
            .Create();

        // act
        builder.TryGetType<CustomHandlerFilterInputType>(
            "TestName",
            out var type);

        // assert
        Assert.NotNull(type);
        Assert.IsType<CustomHandler>(
            Assert.IsType<FilterField>(type!.Fields["friends"]).Handler);
        Assert.IsType<QueryableDefaultFieldHandler>(
            Assert.IsType<FilterField>(type.Fields["name"]).Handler);
    }

    [Fact]
    public void FilterInputType_Should_IgnoreFieldWithoutCallingConvention()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddFiltering(
                x => x.AddDefaultOperations()
                    .BindRuntimeType<string, StringOperationFilterInputType>()
                    .Provider(new QueryableFilterProvider(y => y.AddDefaultFieldHandlers())))
            .AddQueryType(
                new ObjectType(
                    x => x.Name("Query")
                        .Field("foo")
                        .Resolve(new List<IgnoreTest>())
                        .UseFiltering<IgnoreTestFilterInputType>()));

        // act
        var schema = builder.Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Should_InfereType_When_ItIsAInterface()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType<TestingType<ITest<Foo>>>()
            .AddObjectType<ITest<Foo>>();

        // act
        var schema = builder.Create();

        // assert
        schema.ToString().MatchSnapshot();
        schema.Print().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_AllowOnlyCertainOperations()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Foo>(
            x => x.BindFieldsExplicitly()
                .Field(x => x.Bar)
                .AllowOperation(DefaultFilterOperations.NotEquals)
                .AllowOperation(DefaultFilterOperations.Equals));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_ConfigureNestedType()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(x => x
            .BindFieldsExplicitly()
            .Field(x => x.Author, d => d.Field(x => x.Name)));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_ConfigureNestedTypeWithNestedAllows()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(x => x
            .BindFieldsExplicitly()
            .Field(x => x.Author, d => d.Field(x => x.Name).AllowEquals().AllowNotEquals()));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_RenameTypes()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(descriptor =>
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Chapters, d => d.Name("ChaptersInput")).AllowEquals();
            descriptor.Field(
                x => x.Author,
                d => d.Name("AuthorInput").Field(x => x.Id));
        });

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_AllowAndAllowOr()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(descriptor =>
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Chapters).AllowAnd().AllowOr();
            descriptor.Field(
                x => x.Author,
                d => d.AllowAnd().AllowOr().Field(x => x.Id));
        });

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_OnlyConfigureNameOfOperation()
    {
        // arrange
        // act
        void Call() => CreateSchemaWithFilter<Book>(descriptor =>
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Chapters, x => x.Name("OperationName"));
        });

        // assert
        var ex = Assert.Throws<SchemaException>(Call);
        ex.Errors
            .Single(x => x.Code == ErrorCodes.Data.InlineFilterTypeNoFields)
            .Message
            .MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_AddDirective()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(descriptor =>
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Chapters, d => d.Directive("Foobar")).AllowEquals();
            descriptor.Field(
                x => x.Author,
                d => d.Directive("Foobar").Field(x => x.Id));
        }, x => x.AddDirectiveType(
            new DirectiveType(x => x
                .Name("Foobar")
                .Location(Types.DirectiveLocation.InputObject))));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_SetTypeDescription()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(descriptor =>
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Chapters, d => d.Description("Test")).AllowEquals();
            descriptor.Field(
                x => x.Author,
                d => d.Description("Test").Field(x => x.Id));
        });

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_SwitchToImplicit()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(descriptor =>
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Author, d => d.BindFieldsImplicitly());
        });

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_ConfigureDeepNestedTypeWithNestedAllows()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(descriptor =>
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Chapters).AllowEquals().AllowNotEquals();
            descriptor.Field(x => x.Author, authorDescriptor =>
            {
                authorDescriptor.Field(x => x.Id).AllowEquals().AllowNotEquals();
                authorDescriptor.Field(x => x.Name).AllowIn().AllowNotIn().AllowContains();
                authorDescriptor.Field(x => x.Address, descriptor =>
                {
                    descriptor.Field(x => x.PostalCode).AllowEquals().AllowNotEquals();
                    descriptor.Field(
                        x => x.Country,
                        descriptor => descriptor.Field(x => x.Name).AllowIn());
                });
            });
        });

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_ObjectList()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(descriptor =>
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(
                x => x.CoAuthors,
                descriptor => descriptor.AllowAll(d => d.Field(y => y.Name).AllowEquals()));
        });

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_ObjectList_AllOperations()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(descriptor =>
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(
                x => x.CoAuthors,
                descriptor =>
                {
                    descriptor.AllowAll(d => d.Field(y => y.Name).AllowEquals());
                    descriptor.AllowAny();
                    descriptor.AllowNone(d => d.Field(y => y.Name).AllowEquals());
                    descriptor.AllowSome(d => d.Field(y => y.Name).AllowEquals());
                    descriptor.AllowAnd();
                    descriptor.AllowOr();
                });
        });

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_ScalarList()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(descriptor =>
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(
                x => x.LinesPerPage,
                descriptor => descriptor.AllowAll().AllowEquals());
        });

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Inline_ScalarList_AllOperations()
    {
        // arrange
        // act
        var schema = CreateSchemaWithFilter<Book>(descriptor =>
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(
                x => x.LinesPerPage,
                descriptor =>
                {
                    descriptor.AllowAll().AllowEquals();
                    descriptor.AllowAny();
                    descriptor.AllowNone().AllowEquals();
                    descriptor.AllowSome().AllowEquals();
                    descriptor.AllowAnd();
                    descriptor.AllowOr();
                });
        });

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Should_Assert_When_TryToCustomizeNonFilterType()
    {
        // arrange
        // act
        void Call() => CreateSchemaWithFilter<Book>(descriptor =>
             descriptor.Operation(DefaultFilterOperations.Data).Type<StringType>().AllowEquals());

        // assert
        var ex = Assert.Throws<SchemaException>(Call);
        ex.Errors.Single().Message.MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Should_Assert_When_NoTypeWasDefined()
    {
        // arrange
        // act
        void Call() => CreateSchemaWithFilter<Book>(descriptor =>
             descriptor.Operation(DefaultFilterOperations.Data).AllowEquals());

        // assert
        var ex = Assert.Throws<SchemaException>(Call);
        ex.Errors.Single().Message.MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Should_Assert_When_NoFieldsOnOperationType()
    {
        // arrange
        // act
        void Call() => CreateSchemaWithFilter<Book>(descriptor =>
             descriptor.Operation(DefaultFilterOperations.Data).AllowEquals());

        // assert
        var ex = Assert.Throws<SchemaException>(Call);
        ex.Errors.Single().Message.MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Should_Assert_When_CustomOperationFieldDoesNotAllowAnyOperations()
    {
        // arrange
        // act
        void Call() => CreateSchemaWithFilter<Book>(descriptor =>
             descriptor.Field(x => x.Title, x => x.Name("CustomName")));

        // assert
        var ex = Assert.Throws<SchemaException>(Call);
        ex.Errors.Single().Message.MatchSnapshot();
    }

    [Fact]
    public void FilterInputType_Should_Assert_When_CustomnFieldDoesNotAllowAnyFields()
    {
        // arrange
        // act
        void Call() => CreateSchemaWithFilter<Book>(descriptor =>
             descriptor.Field(x => x.Author, x => x.Name("CustomName")));

        // assert
        var ex = Assert.Throws<SchemaException>(Call);
        ex.Errors.Single().Message.MatchSnapshot();
    }

    public class FooDirectiveType
        : DirectiveType<FooDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<FooDirective> descriptor)
        {
            descriptor.Name("foo");
            descriptor.Location(Types.DirectiveLocation.InputObject)
                .Location(Types.DirectiveLocation.InputFieldDefinition);
        }
    }

    public class ExampleFilterinputType : FilterInputType<Book>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Book> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Chapters, x => x.Name("asd")).AllowEquals().AllowNotEquals();
            descriptor.Field(x => x.Author, authorDescriptor =>
            {
                authorDescriptor.Field(x => x!.Id).AllowEquals().AllowNotEquals();
                authorDescriptor.Field(x => x!.Name).AllowIn().AllowNotIn().AllowContains();
                authorDescriptor.Field(x => x!.Address, descriptor =>
                {
                    descriptor.Field(x => x!.PostalCode).AllowEquals().AllowNotEquals();
                    descriptor.Field(
                        x => x!.Country,
                        descriptor => descriptor.Field(x => x.Name).AllowIn());
                });
            });
        }
    }

    public class FooDirective
    {
    }

    public class Foo
    {
        public string Bar { get; set; } = default!;
    }

    public class Bar
    {
        public string Baz { get; set; } = default!;

        public string Qux { get; set; } = default!;
    }

    public class Query
    {
        [GraphQLNonNullType]
        public IQueryable<Book> Books() => new List<Book>().AsQueryable();
    }

    public class Book
    {
        public int Id { get; set; }

        [GraphQLNonNullType]
        public string? Title { get; set; }

        public int Pages { get; set; }
        public int Chapters { get; set; }

        public int[] LinesPerPage { get; set; } = Array.Empty<int>();

        public ICollection<Author>? CoAuthors { get; set; }

        [GraphQLNonNullType]
        public Author? Author { get; set; }
    }

    public class Author
    {
        [GraphQLType(typeof(NonNullType<IdType>))]
        public int Id { get; set; }

        [GraphQLNonNullType]
        public string? Name { get; set; }

        public Address? Address { get; set; }
    }

    public class Address
    {
        public string? Street { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public Country? Country { get; set; }
    }

    public class Country
    {
        public string? Name { get; set; }
    }

    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public List<User> Friends { get; set; } = default!;
    }

    public interface ITest
    {
        public string? Prop { get; set; }
        public string? Prop2 { get; set; }
    }

    public interface ITest<T>
    {
        T Prop { get; set; }
    }

    public class InterfaceImpl1 : ITest
    {
        public string? Prop { get; set; }

        public string? Prop2 { get; set; }
    }

    public class IgnoreTest
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
    }

    public class IgnoreTestFilterInputType
        : FilterInputType<IgnoreTest>
    {
        protected override void Configure(IFilterInputTypeDescriptor<IgnoreTest> descriptor)
        {
            descriptor.Ignore(x => x.Id);
        }
    }

    public class UserFilterInput : FilterInputType<User>
    {
        protected override void Configure(IFilterInputTypeDescriptor<User> descriptor)
        {
            descriptor.Ignore(x => x.Id);
        }
    }

    public class UserQueryType : ObjectType<User>
    {
        protected override void Configure(IObjectTypeDescriptor<User> descriptor)
        {
            descriptor.Name(nameof(Query));
            descriptor
                .Field("foo")
                .Resolve(new List<User>())
                .UseFiltering<UserFilterInput>();
        }
    }

    public class CustomHandlerFilterInputType : FilterInputType<User>
    {
        protected override void Configure(IFilterInputTypeDescriptor<User> descriptor)
        {
            descriptor.Name("TestName");
            descriptor.Field(x => x.Id)
                .Extend()
                .OnBeforeCreate(x => x.Handler = new CustomHandler());

            descriptor.Field(x => x.Friends)
                .Extend()
                .OnBeforeCompletion((ctx, x) => x.Handler = new CustomHandler());
        }
    }

    public class CustomHandlerQueryType : ObjectType<User>
    {
        protected override void Configure(IObjectTypeDescriptor<User> descriptor)
        {
            descriptor.Name(nameof(Query));
            descriptor
                .Field("foo")
                .Resolve(new List<User>())
                .UseFiltering<CustomHandlerFilterInputType>();
        }
    }

    public class TestObject<T>
    {
        public T Root { get; set; } = default!;
    }

    public class TestingType<T> : ObjectType<TestObject<T>>
    {
        protected override void Configure(IObjectTypeDescriptor<TestObject<T>> descriptor)
        {
            descriptor.Name(nameof(Query));
            descriptor.Field(x => x.Root).UseFiltering();
        }
    }

    public class CustomHandler : IFilterFieldHandler
    {
        public bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            throw new NotImplementedException();
        }
    }
}
