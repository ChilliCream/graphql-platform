using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class TypeValidationTests
    {
        [Fact]
        public void EnsurePagingIsFirst()
        {
            void Action() =>
                SchemaBuilder.New()
                    .AddQueryType<InvalidMiddlewarePipeline1>()
                    .AddProjections()
                    .AddFiltering()
                    .AddSorting()
                    .Create();

            Assert.Throws<SchemaException>(Action).Message.MatchSnapshot();
        }

        [Fact]
        public void EnsureProjectionComesAfterDbContext()
        {
            void Action() =>
                SchemaBuilder.New()
                    .AddQueryType<InvalidMiddlewarePipeline1>()
                    .AddProjections()
                    .AddFiltering()
                    .AddSorting()
                    .Create();

            Assert.Throws<SchemaException>(Action).Message.MatchSnapshot();
        }

        public class InvalidMiddlewarePipeline1
        {
            [UseFiltering]
            [UsePaging]
            public IEnumerable<string> GetBars() => throw new NotImplementedException();
        }

        public class InvalidMiddlewarePipeline2
        {
            [UsePaging]
            [UseFiltering]
            [UseProjection]
            public IEnumerable<string> GetBars() => throw new NotImplementedException();
        }
    }
}
