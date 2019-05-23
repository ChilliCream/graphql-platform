using System;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public class Query
    {
        private readonly TestService _service;
        private readonly IHttpContextAccessor _accessor;

        public Query(TestService testService, IHttpContextAccessor accessor)
        {
            _service = testService
                ?? throw new ArgumentNullException(nameof(testService));
            _accessor = accessor
                ?? throw new ArgumentNullException(nameof(accessor));
        }

        public string SayHello()
        {
            return _service.GetGreetings();
        }

        public string GetRequestPath()
        {
            return _accessor.HttpContext.Request.Path;
        }

        public string GetRequestPath2(IResolverContext context)
        {
            return context.Service<IHttpContextAccessor>()
                .HttpContext.Request.Path;
        }

        public string GetRequestPath3([Service]IHttpContextAccessor accessor)
        {
            return accessor.HttpContext.Request.Path;
        }

        public Foo GetBasic()
        {
            return new Foo
            {
                A = "1",
                B = "2",
                C = 3
            };
        }

        public Foo GetWithScalarArgument(string a)
        {
            return new Foo
            {
                A = a,
                B = "2",
                C = 3
            };
        }

        public Foo GetWithObjectArgument(Foo b)
        {
            return new Foo
            {
                A = b.A,
                B = "2",
                C = b.C
            };
        }

        public bool GetWithEnum(TestEnum test)
        {
            return true;
        }

        public TestEnum GetWithNestedEnum(Bar bar)
        {
            return bar.A;
        }
    }
}
