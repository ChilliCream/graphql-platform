﻿using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Snapshooter.Xunit;
using Xunit;

namespace Types.Tests.Configuration
{
    public class MiddlewareConfigurationTests
    {
        [Fact]
        public async Task MiddlewareConfig_MapWithDelegate()
        {
            // arrange
            ISchema schema = Schema.Create(
                "type Query { a: String b: String }",
                c => c.Map(
                        new FieldReference("Query", "a"),
                        next => context =>
                        {
                            context.Result = "123";
                            return Task.CompletedTask;
                        })
                    .Map(
                        new FieldReference("Query", "b"),
                        next => context =>
                        {
                            context.Result = "456";
                            return Task.CompletedTask;
                        }));

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a b }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task MiddlewareConfig_MapWithClass()
        {
            // arrange
            ISchema schema = Schema.Create(
                "type Query { a: String b: String }",
                c => c.Map<TestFieldMiddleware>(
                        new FieldReference("Query", "a"))
                    .Map(
                        new FieldReference("Query", "b"),
                        next => context =>
                        {
                            context.Result = "456";
                            return Task.CompletedTask;
                        }));

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a b }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task MiddlewareConfig_MapWithClassFactory()
        {
            // arrange
            ISchema schema = Schema.Create(
                "type Query { a: String b: String }",
                c => c.Map<TestFieldMiddleware>(
                        new FieldReference("Query", "a"),
                        (services, next) => new TestFieldMiddleware(next))
                    .Map(
                        new FieldReference("Query", "b"),
                        next => context =>
                        {
                            context.Result = "456";
                            return Task.CompletedTask;
                        }));

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a b }");

            // assert
            result.MatchSnapshot();
        }

        public class TestFieldMiddleware
        {
            private FieldDelegate _next;

            public TestFieldMiddleware(FieldDelegate next)
            {
                _next = next ?? throw new ArgumentNullException(nameof(next));
            }

            public Task InvokeAsync(IMiddlewareContext context)
            {
                context.Result = "123456789";
                return _next(context);
            }
        }
    }
}
