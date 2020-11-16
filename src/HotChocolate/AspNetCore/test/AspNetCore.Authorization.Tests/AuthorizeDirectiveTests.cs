using System;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore.Authorization
{
    public class AuthorizeDirectiveTests
    {
        [Fact]
        public void CreateInstance_PolicyRoles_PolicyIsNullRolesHasItems()
        {
            // arrange
            // act
            var authorizeDirective = new AuthorizeDirective(
                null,
                new[] { "a", "b" });

            // assert
            Assert.Null(authorizeDirective.Policy);
            Assert.Collection(authorizeDirective.Roles,
                t => Assert.Equal("a", t),
                t => Assert.Equal("b", t));
        }

        [Fact]
        public void CreateInstance_PolicyRoles_PolicyIsSetRolesIsEmpty()
        {
            // arrange
            // act
            var authorizeDirective = new AuthorizeDirective(
                "abc", Array.Empty<string>());

            // assert
            Assert.Equal("abc", authorizeDirective.Policy);
            Assert.Empty(authorizeDirective.Roles);
        }

        [Fact]
        public void CreateInstance_PolicyRoles_PolicyIsSetRolesIsNull()
        {
            // arrange
            // act
            var authorizeDirective = new AuthorizeDirective(
                "abc", Array.Empty<string>());

            // assert
            Assert.Equal("abc", authorizeDirective.Policy);
            Assert.Empty(authorizeDirective.Roles);
        }

        [Fact]
        public void CreateInstance_Policy_PolicyIsSet()
        {
            // arrange
            // act
            var authorizeDirective = new AuthorizeDirective("abc");

            // assert
            Assert.Equal("abc", authorizeDirective.Policy);
            Assert.Null(authorizeDirective.Roles);
        }

        [Fact]
        public void CreateInstance_Roles_RolesHasItems()
        {
            // arrange
            // act
            var authorizeDirective = new AuthorizeDirective(
                new[] { "a", "b" });

            // assert
            Assert.Null(authorizeDirective.Policy);
            Assert.Collection(authorizeDirective.Roles,
                t => Assert.Equal("a", t),
                t => Assert.Equal("b", t));
        }

        [Fact]
        public void TypeAuth_DefaultPolicy()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Authorize()
                    .Field("foo")
                    .Resolver("bar"))
                .AddAuthorizeDirectiveType()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void TypeAuth_DefaultPolicy_DescriptorNull()
        {
            // arrange
            // act
            Action action = () =>
                AuthorizeObjectTypeDescriptorExtensions.Authorize(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void TypeAuth_WithPolicy()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Authorize("MyPolicy")
                    .Field("foo")
                    .Resolver("bar"))
                .AddAuthorizeDirectiveType()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void TypeAuth_WithPolicy_DescriptorNull()
        {
            // arrange
            // act
            Action action = () =>
                AuthorizeObjectTypeDescriptorExtensions
                    .Authorize(null, "MyPolicy");

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void TypeAuth_WithRoles()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Authorize(new[] { "MyRole" })
                    .Field("foo")
                    .Resolver("bar"))
                .AddAuthorizeDirectiveType()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void TypeAuth_WithRoles_DescriptorNull()
        {
            // arrange
            // act
            Action action = () =>
                AuthorizeObjectTypeDescriptorExtensions
                    .Authorize(null, new[] { "MyRole" });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void FieldAuth_DefaultPolicy()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Authorize()
                    .Resolver("bar"))
                .AddAuthorizeDirectiveType()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FieldAuth_DefaultPolicy_AfterResolver()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Authorize(apply: ApplyPolicy.AfterResolver)
                    .Resolver("bar"))
                .AddAuthorizeDirectiveType()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FieldAuth_DefaultPolicy_BeforeResolver()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Authorize(apply: ApplyPolicy.BeforeResolver)
                    .Resolver("bar"))
                .AddAuthorizeDirectiveType()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FieldAuth_DefaultPolicy_DescriptorNull()
        {
            // arrange
            // act
            Action action = () =>
                AuthorizeObjectFieldDescriptorExtensions
                    .Authorize(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void FieldAuth_WithPolicy()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Authorize("MyPolicy")
                    .Resolver("bar"))
                .AddAuthorizeDirectiveType()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FieldAuth_WithPolicy_AfterResolver()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Authorize("MyPolicy", apply: ApplyPolicy.AfterResolver)
                    .Resolver("bar"))
                .AddAuthorizeDirectiveType()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FieldAuth_WithPolicy_BeforeResolver()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Authorize("MyPolicy", apply: ApplyPolicy.BeforeResolver)
                    .Resolver("bar"))
                .AddAuthorizeDirectiveType()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FieldAuth_WithPolicy_DescriptorNull()
        {
            // arrange
            // act
            Action action = () =>
                AuthorizeObjectFieldDescriptorExtensions
                    .Authorize(null, "MyPolicy");

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void FieldAuth_WithRoles()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Authorize(new[] { "MyRole" })
                    .Resolver("bar"))
                .AddAuthorizeDirectiveType()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FieldAuth_WithRoles_DescriptorNull()
        {
            // arrange
            // act
            Action action = () =>
                AuthorizeObjectFieldDescriptorExtensions
                    .Authorize(null, new[] { "MyRole" });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
