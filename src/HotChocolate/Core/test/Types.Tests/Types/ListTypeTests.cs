﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class ListTypeTests
    {
        [Fact]
        public void EnsureElementTypeIsCorrectlySet()
        {
            // arrange
            var innerType = new StringType();

            // act
            var type = new ListType(innerType);

            // assert
            Assert.Equal(innerType, type.ElementType);
        }


        [Fact]
        public void EnsureNonNullElementTypeIsCorrectlySet()
        {
            // arrange
            var innerType = new NonNullType(new StringType());

            // act
            var type = new ListType(innerType);

            // assert
            Assert.Equal(innerType, type.ElementType);
        }

        [Fact]
        public void EnsureNativeTypeIsCorrectlyDetected()
        {
            // arrange
            var innerType = new NonNullType(new StringType());
            var type = new ListType(innerType);

            // act
            Type clrType = type.RuntimeType;

            // assert
            Assert.Equal(typeof(List<string>), clrType);
        }

        [Fact]
        public async Task Integration_List_ListValues_Scalars()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery("{ scalars(values: [1,2]) }")
                    .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Integration_List_ScalarValue_Scalars()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery("{ scalars(values: 1) }")
                    .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Integration_List_ListValues_Object()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery("{ objects(values: [{ bar: 1 }, { bar: 2 }]) { bar } }")
                    .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Integration_List_ScalarValue_Object()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery("{ objects(values: { bar: 1 }) { bar } }")
                    .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        public class Query
        {
            public int[] Scalars(int[] values) => values;

            public Foo[] Objects(Foo[] values) => values;
        }

        public class Foo
        {
            public int Bar { get; set; }
        }
    }
}
