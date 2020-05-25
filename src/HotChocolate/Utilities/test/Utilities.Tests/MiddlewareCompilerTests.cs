using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Utilities
{
    public class MiddlewareCompilerTests
    {
        [Fact]
        public void CompileFactory()
        {
            // arrange
            // act
            MiddlewareFactory<CustomClassMiddleware, IServiceProvider, CustomDelegate> factory =
                MiddlewareCompiler<CustomClassMiddleware>
                    .CompileFactory<IServiceProvider, CustomDelegate>(
                        (services, next) =>
                        new List<IParameterHandler>
                        {
                            new TypeParameterHandler(typeof(string), Expression.Constant("abc")),
                            new ServiceParameterHandler(services)
                        });
            
            // assert
            CustomClassMiddleware middleware = 
                factory.Invoke(new EmptyServiceProvider(), c => default);
            Assert.Equal("abc", middleware.Some);
        }

        [Fact]
        public void CompileDelegate()
        {
            // arrange
            MiddlewareFactory<CustomClassMiddleware, IServiceProvider, CustomDelegate> factory =
                MiddlewareCompiler<CustomClassMiddleware>
                    .CompileFactory<IServiceProvider, CustomDelegate>(
                        (services, next) =>
                        new List<IParameterHandler>
                        {
                            new TypeParameterHandler(typeof(string), Expression.Constant("abc")),
                            new ServiceParameterHandler(services)
                        });

            CustomClassMiddleware middleware = 
                factory.Invoke(new EmptyServiceProvider(), c => default);

            // act
            ClassQueryDelegate<CustomClassMiddleware, CustomContext> pipeline =  
                MiddlewareCompiler<CustomClassMiddleware>.CompileDelegate<CustomContext>(
                    (context, middleware) =>
                    new List<IParameterHandler>
                    {
                        new TypeParameterHandler(typeof(string), Expression.Constant("def"))
                    });
            
            // assert
            var context = new CustomContext(new EmptyServiceProvider());
            pipeline.Invoke(context, middleware);
            Assert.Equal("abcdef", context.Result);
        }

        public class CustomClassMiddleware
        {
            private readonly CustomDelegate _next;

            public CustomClassMiddleware(CustomDelegate next, string some)
            {
                _next = next;
                Some = some;
            }

            public string Some { get; }

            public async Task InvokeAsync(CustomContext context, string some)
            {
                context.Result = Some + some;
                await _next(context);
            }
        }

        public class CustomContext
        {
            public CustomContext(IServiceProvider services)
            {
                Services = services;
            }

            public IServiceProvider Services { get; }

            public string Result { get; set; }
        }

        public delegate ValueTask CustomDelegate(CustomContext context);

        public delegate CustomContext CustomMiddleware(CustomDelegate next);

    }
}
