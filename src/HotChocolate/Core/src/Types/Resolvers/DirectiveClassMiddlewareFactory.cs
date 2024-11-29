#nullable enable

using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers;

internal static class DirectiveClassMiddlewareFactory
{
    private static readonly MethodInfo _createGeneric =
        typeof(DirectiveClassMiddlewareFactory)
            .GetTypeInfo().DeclaredMethods.First(
                t =>
                {
                    if (t.Name.EqualsOrdinal(nameof(Create)) && t.GetGenericArguments().Length == 1)
                    {
                        return t.GetParameters().Length == 0;
                    }
                    return false;
                });

    private static readonly PropertyInfo _services =
        typeof(IResolverContext).GetProperty(nameof(IResolverContext.Services))!;

    internal static DirectiveMiddleware Create<TMiddleware>()
        where TMiddleware : class
    {
        var sync = new object();
        MiddlewareFactory<TMiddleware, IServiceProvider, FieldDelegate>? activate = null;
        ClassQueryDelegate<TMiddleware, IMiddlewareContext>? invoke = null;

        return (next, directive) =>
        {
            if (invoke is null || activate is null)
            {
                lock (sync)
                {
                    if (invoke is null || activate is null)
                    {
                        var directiveHandler = new DirectiveParameterHandler(directive);

                        activate =
                            MiddlewareCompiler<TMiddleware>
                                .CompileFactory<IServiceProvider, FieldDelegate>(
                                    (services, _) => new IParameterHandler[]
                                    {
                                        directiveHandler, new ServiceParameterHandler(services),
                                    });

                        invoke =
                            MiddlewareCompiler<TMiddleware>
                                .CompileDelegate<IMiddlewareContext>(
                                    (context, _) => new List<IParameterHandler>
                                    {
                                        directiveHandler,
                                        new ServiceParameterHandler(
                                            Expression.Property(context, _services)),
                                    });
                    }
                }
            }

            TMiddleware? instance = null;

            return context =>
            {
                instance ??= activate(context.Services, next);
                return invoke(context, instance);
            };
        };
    }

    internal static DirectiveMiddleware Create(Type middlewareType)
        => (DirectiveMiddleware)_createGeneric
            .MakeGenericMethod(middlewareType)
            .Invoke(null, [])!;

    internal static DirectiveMiddleware Create<TMiddleware>(
        Func<IServiceProvider, FieldDelegate, TMiddleware> activate)
        where TMiddleware : class
    {
        var sync = new object();
        ClassQueryDelegate<TMiddleware, IMiddlewareContext>? invoke = null;

        return (next, directive) =>
        {
            if (invoke is null)
            {
                lock (sync)
                {
                    if (invoke is null)
                    {
                        var directiveHandler = new DirectiveParameterHandler(directive);

                        invoke =
                            MiddlewareCompiler<TMiddleware>
                                .CompileDelegate<IMiddlewareContext>(
                                    (context, _) => new List<IParameterHandler>
                                    {
                                        directiveHandler,
                                        new ServiceParameterHandler(
                                            Expression.Property(context, _services)),
                                    });
                    }
                }
            }

            TMiddleware? instance = null;

            return context =>
            {
                instance ??= activate(context.Services, next);
                return invoke(context, instance);
            };
        };
    }

    private sealed class DirectiveParameterHandler : IParameterHandler
    {
        private readonly Directive _directive;
        private readonly Type _runtimeType;

        public DirectiveParameterHandler(Directive directive)
        {
            _directive = directive;
            _runtimeType = directive.Type.RuntimeType;
        }

        public bool CanHandle(ParameterInfo parameter)
            => parameter.ParameterType == typeof(Directive) ||
                parameter.ParameterType == typeof(DirectiveNode) ||
                (_runtimeType != typeof(object) && _runtimeType == parameter.ParameterType);

        public Expression CreateExpression(ParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(Directive))
            {
                return Expression.Constant(_directive, typeof(Directive));
            }

            if (parameter.ParameterType == typeof(DirectiveNode))
            {
                return Expression.Constant(_directive.AsSyntaxNode(), typeof(DirectiveNode));
            }

            return  Expression.Constant(_directive.AsValue<object>(), _runtimeType);
        }
    }
}
