using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Test
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

            SchemaException exception = Assert.Throws<SchemaException>(Action);

            Assert.Collection(
                exception.Errors,
                error => Assert.Equal("HC0050", error.Code));

            exception.Message.MatchSnapshot();
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

            SchemaException exception = Assert.Throws<SchemaException>(Action);

            Assert.Collection(
                exception.Errors,
                error => Assert.Equal("HC0050", error.Code));
            
            exception.Message.MatchSnapshot();
        }

        [Fact]
        public void EnsureCorrectlyOrderedMiddlewarePassValidation()
        {
            SchemaBuilder.New()
                .AddQueryType<CorrectMiddlewarePipeline>()
                .AddProjections()
                .AddFiltering()
                .AddSorting()
                .Create()
                .ToString()
                .MatchSnapshot();
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

        public class CorrectMiddlewarePipeline
        {
            [UsePaging]
            [UseSomeOther]
            [UseProjection]
            [UseSomeOther]
            [UseFiltering]
            [UseSomeOther]
            [UseSorting]
            [UseSomeOther]
            public IEnumerable<Foo> GetBars() => throw new NotImplementedException();
        }

        public class Foo
        {
            public string? Bar { get; set; }
        }

        public class UseSomeOtherAttribute : ObjectFieldDescriptorAttribute
        {
            public override void OnConfigure(
                IDescriptorContext context,
                IObjectFieldDescriptor descriptor,
                MemberInfo member)
            {
                descriptor.Use(_ => _);
            }
        }
    }
}
