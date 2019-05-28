using System.Reflection;
using System;
using System.Collections.Generic;
using Xunit;

namespace HotChocolate.AspNetCore.Authorization
{
    public class AuthorizeDirectiveTests
    {
        [Fact]
        public void CreateInstance_Policy_PolicyIsNull()
        {
            // arrange
            // act
            Action action = () => new AuthorizeDirective((string)null);

            // assert
            Assert.Equal(
                "Either policy or roles has to be set.",
                Assert.Throws<ArgumentException>(action).Message);
        }

        [Fact]
        public void CreateInstance_Roles_RolesIsNull()
        {
            // arrange
            // act
            Action action = () => new AuthorizeDirective(
                (IReadOnlyCollection<string>)null);

            // assert
            Assert.Equal(
                "Either policy or roles has to be set.",
                Assert.Throws<ArgumentException>(action).Message);
        }

        [Fact]
        public void CreateInstance_PolicyRoles_PolicyIsNullRolesIsNull()
        {
            // arrange
            // act
            Action action = () => new AuthorizeDirective(null, null);

            // assert
            Assert.Equal(
                "Either policy or roles has to be set.",
                Assert.Throws<ArgumentException>(action).Message);
        }

        [Fact]
        public void CreateInstance_PolicyRoles_PolicyIsNullRolesIsEmpty()
        {
            // arrange
            // act
            Action action = () => new AuthorizeDirective(
                null,
                Array.Empty<string>());

            // assert
            Assert.Equal(
                "Either policy or roles has to be set.",
                Assert.Throws<ArgumentException>(action).Message);
        }

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
            Assert.Empty(authorizeDirective.Roles);
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
    }
}
