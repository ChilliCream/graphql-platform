using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus
{
    public interface IResolverBuilder
    {
        IResolverBuilder Add(string typeName, Func<IResolverContext, object> resolver);
        IResolverBuilder Add(string typeName, Func<IResolverContext, CancellationToken, Task<object>> resolver);
        IResolverBuilder Add(string typeName, string fieldName, Func<IResolverContext, object> resolver);
        IResolverBuilder Add(string typeName, string fieldName, Func<IResolverContext, CancellationToken, Task<object>> resolver);

        IResolverCollection Build();
    }

    public static class ResolverBuilderExtensions
    {
        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder,
            string typeName, Func<IResolverContext, Task<object>> resolver)
        {
            return resolverBuilder.Add(typeName, (context, cancellationToken) => resolver(context));
        }

        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder,
           string typeName, string fieldName, Func<IResolverContext, Task<object>> resolver)
        {
            return resolverBuilder.Add(typeName, fieldName, (context, cancellationToken) => resolver(context));
        }
    }

    public interface IResolverContext
    {
        T Argument<T>(string name);

    }

    public interface IResolver
    {
        Task<object> ResolveAsync(IResolverContext context, CancellationToken cancellationToken);
    }

    public interface IResolver<TResult>
        : IResolver
    {
        new Task<TResult> ResolveAsync(IResolverContext context, CancellationToken cancellationToken);
    }

    public interface IFieldResolver
        : IResolver
    {
        string TypeName { get; }
        string FieldName { get; }
    }

     public interface IFieldResolver<TResult>
        : IFieldResolver
        , IResolver<TResult>
    {
    }

    public interface IResolverCollection
    {

    }

    public class Foo
    {
        public void Bar(IResolverBuilder builder)
        {
            IResolverCollection resolvers = builder
                .Add("Query", c => c)
                .Add("Query", "Books", c => c)
                .Add("Query", "DVDs", c => Test(c.Argument<string>("name"), c.Argument<int>("count")))
                .Build();
        }

        public string Test(string a, int x)
        {
            return default(string);
        }
    }
}
