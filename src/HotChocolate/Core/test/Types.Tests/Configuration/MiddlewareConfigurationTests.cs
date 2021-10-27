﻿using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Configuration
{
    public class MiddlewareConfigurationTests
    {
        [Fact]
        public async Task MiddlewareConfig_MapWithDelegate()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String b: String }")
                .Map(new FieldReference("Query", "a"),
                    _ => context =>
                    {
                        context.Result = "123";
                        return default;
                    })
                .Map(new FieldReference("Query", "b"),
                    _ => context =>
                    {
                        context.Result = "456";
                        return default;
                    })
                .Create();

            // act
            IExecutionResult result = await schema.MakeExecutable().ExecuteAsync("{ a b }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task MiddlewareConfig_MapWithClass()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String b: String }")
                .Map<TestFieldMiddleware>(new FieldReference("Query", "a"))
                .Map(new FieldReference("Query", "b"),
                    _ => context =>
                    {
                        context.Result = "456";
                        return default;
                    })
                .Create();

            // act
            IExecutionResult result = await schema.MakeExecutable().ExecuteAsync("{ a b }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task MiddlewareConfig_MapWithClassFactory()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String b: String }")
                .Map(new FieldReference("Query", "a"),
                    (_, next) => new TestFieldMiddleware(next))
                .Map(new FieldReference("Query", "b"),
                    _ => context =>
                    {
                        context.Result = "456";
                        return default;
                    })
                .Create();

            // act
            IExecutionResult result = await schema.MakeExecutable().ExecuteAsync("{ a b }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        public class TestFieldMiddleware
        {
            private readonly FieldDelegate _next;

            public TestFieldMiddleware(FieldDelegate next)
            {
                _next = next ?? throw new ArgumentNullException(nameof(next));
            }

            public ValueTask InvokeAsync(IMiddlewareContext context)
            {
                context.Result = "123456789";
                return _next(context);
            }
        }
    }
}
