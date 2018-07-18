using System;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public interface IResolverContext
    {
        // schema context
        ISchema Schema { get; }

        ObjectType ObjectType { get; }

        ObjectField Field { get; }

        // query context
        DocumentNode QueryDocument { get; }

        OperationDefinitionNode Operation { get; }

        FieldNode FieldSelection { get; }

        // execution context
        ImmutableStack<object> Source { get; } // parents

        Path Path { get; }

        T Parent<T>();

        T Argument<T>(string name);

        T Service<T>();

        T State<T>(StateScope scope);

        T Loader<T>(string key, StateScope scope);
    }

    public static class Foo
    {
        public static void Bar()
        {
            Schema.Create(c =>
            {
                c.RegisterLoader<UserLoader>(l => l.Trigger());
                c.RegisterLoader<UserLoader>();
                c.RegisterLoader<UserLoader>("k", StateScope.User);
                c.RegisterLoader("k", sp => new UserLoader());


                c.RegisterState<Blub>(sp => new Blub(), StateScope.User);
            });
        }
    }

    public static class ResolverContextExtensions
    {
        public static T GlobalState<T>(this IResolverContext resolverContext)
        {
            return resolverContext.State<T>(StateScope.Global);
        }

        public static T UserState<T>(this IResolverContext resolverContext)
        {
            return resolverContext.State<T>(StateScope.User);
        }

        public static T State<T>(this IResolverContext resolverContext)
        {
            return resolverContext.State<T>(StateScope.Request);
        }
    }

    public enum ExecutionScope
    {
        Request,
        User,
        Global
    }
}
