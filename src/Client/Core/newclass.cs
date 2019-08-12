using System.Linq;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Client
{
    public interface IReadOnlyQueryRequest
    {

    }

    public interface IExecutionResult
    {

    }


    public interface IReadOnlyQueryResult
        : IExecutionResult
    {

    }

    public interface IConnection
    {
        Task<IExecutionResult> ExecuteAsync(
            IReadOnlyQueryRequest request,
            CancellationToken cancellationToken);
    }

    public interface IQueryableValue
    {
        Expression Expression { get; }
    }

    public interface IQueryableValue<out T>
        : IQueryableValue
    {
    }

    public interface IQueryableList<out T>
        : IQueryableValue<T>
    {
    }

    public class QueryableValue<T>
        : IQueryableValue<T>
    {
        public Expression Expression => throw new NotImplementedException();
    }

    public static class QueryableExtensions
    {
        public static IQueryableList<TResult> Select<TValue, TResult>(
            this IQueryableList<TValue> source,
            Expression<Func<TValue, TResult>> selector)
                where TValue : IQueryableValue
        {
            return null;
        }

        public static IQueryableValue<TResult> Select<TValue, TResult>(
            this IQueryableValue<TValue> source,
            Expression<Func<TValue, TResult>> selector)
                where TValue : IQueryableValue
        {
            return new QueryableValue<TResult>();
        }
    }

    public static class Demo
    {
        public static void Foo()
        {
            var x = new Query().Select(t => new
            {
                fooFoo = t.Foo.Select(c => new
                {
                    c.Bar
                })
            });

            Type type = x.GetType().GenericTypeArguments.First();
        }
    }



    public class Query
        : IQueryableValue<Query>
    {
        public Expression Expression => throw new NotImplementedException();

        public Foo Foo { get; }
    }

    public class Foo
        : IQueryableValue<Foo>
    {
        public Expression Expression => throw new NotImplementedException();

        public string Bar { get; }
    }

    public class Bar : IQueryableValue<Bar>
    {
        public Expression Expression => throw new NotImplementedException();
    }




    public interface IQueryCompiler
    {
        // ICompiledQuery<T> Compile(IQueryableValue queryable);
    }

    public interface IStreamCompiler
    {
        // ICompiledQuery<T> Compile(IQueryableValue queryable);
    }

    public interface ICompiledQuery<T>
    {
        Task<T> ExecuteAsync(CancellationToken cancellationToken);
    }

    // batch => response stream
    // sub => res stream

}
