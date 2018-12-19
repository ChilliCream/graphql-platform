using System;
using HotChocolate.Resolvers;
using Microsoft.Owin;

namespace HotChocolate.AspNetClassic
{
    public class Query
    {
        private readonly TestService _service;
        private readonly IOwinContext _context;

        public Query(TestService testService, IOwinContext context)
        {
            _service = testService
                ?? throw new ArgumentNullException(nameof(testService));
            _context = context
                ?? throw new ArgumentNullException(nameof(context));
        }

        public string SayHello()
        {
            return _service.GetGreetings();
        }

        public string GetRequestPath()
        {
            return _context.Request.Path.Value;
        }

        public string GetRequestPath2(IResolverContext context)
        {
            return context.Service<IOwinContext>().Request.Path.Value;
        }

        public string GetRequestPath3([Service]IOwinContext context)
        {
            return context.Request.Path.Value;
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
