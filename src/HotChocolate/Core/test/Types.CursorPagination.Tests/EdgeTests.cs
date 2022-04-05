﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Pagination
{
    public class EdgeTests
    {
        [InlineData("abc", "cde")]
        [InlineData("cde", null)]
        [Theory]
        public void CreateEdge_ArgumentsArePassedCorrectly(
            string cursor, string node)
        {
            // arrange
            // act
            var edge = new Edge<string>(node, cursor);

            // assert
            Assert.Equal(cursor, edge.Cursor);
            Assert.Equal(node, edge.Node);
        }

        [Fact]
        public void CreateEdge_CursorIsNull_ArgumentNullException()
        {
            // arrange
            // act
            void Action() => new Edge<string>("abc", null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void CreateEdge_CursorIsEmpty_ArgumentNullException()
        {
            // arrange
            // act
            void Action() => new Edge<string>("abc", string.Empty);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public async Task Extend_Edge_Type_And_Inject_Edge_Value_Schema()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddTypeExtension<UsersEdgeExtensions>()
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Extend_Edge_Type_And_Inject_Edge_Value_Request()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddTypeExtension<UsersEdgeExtensions>()
                .ExecuteRequestAsync("{ users { edges { test } } }")
                .MatchSnapshotAsync();
        }

        public class Query
        {
            [UsePaging]
            public IEnumerable<User> GetUsers() => new[] { new User { Name = "Hello" } };
        }

        [ExtendObjectType("UsersEdge")]
        public class UsersEdgeExtensions
        {
            public string Test([Parent] Edge<User> edge)
            {
                return edge.Node.Name;
            }
        }

        public class User
        {
            public string Name { get; set; }
        }
    }
}
