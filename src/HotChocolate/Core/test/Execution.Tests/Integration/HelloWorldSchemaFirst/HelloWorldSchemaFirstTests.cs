﻿using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.HelloWorldSchemaFirst
{
    public class HelloWorldSchemaFirstTests
    {
        [Fact]
        public async Task SimpleHelloWorldWithoutTypeBinding()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ hello }",
                c => c
                    .AddDocumentFromString(
                        @"
                        type Query {
                            hello: String
                        }")
                    .AddResolver("Query", "hello", () => "world"))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task SimpleHelloWorldWithArgumentWithoutTypeBinding()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ hello(a: \"foo\") }",
                c => c
                    .AddDocumentFromString(
                        @"
                        type Query {
                            hello(a: String!): String
                        }")
                    .AddResolver("Query", "hello", ctx => ctx.ArgumentValue<string>("a")))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task SimpleHelloWorldWithResolverType()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ hello world }",
                c => c
                    .AddDocumentFromString(
                        @"
                        type Query {
                            hello: String
                            world: String
                        }")
                    .AddResolver<QueryA>("Query")
                    .AddResolver<QueryB>("Query"))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task SimpleHelloWorldWithResolverTypeAndArgument()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ hello(a: \"foo_\") }",
                c => c
                    .AddDocumentFromString(
                        @"type Query {
                            hello(a: String!): String
                        }")
                    .AddResolver<QueryC>("Query"))
                .MatchSnapshotAsync();
        }

        public class QueryA
        {
            public string Hello => "World";
        }

        public class QueryB
        {
            public string World => "Hello";
        }

        public class QueryC
        {
            public string GetHello(string a, IResolverContext context)
            {
                return a + context.ArgumentValue<string>("a");
            }
        }
    }
}
