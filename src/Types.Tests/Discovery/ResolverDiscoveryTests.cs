using System;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Discovery
{
    public class ResolverDiscoveryTests
    {
        [Fact]
        public void DiscoverQueryResolvers()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<Query>();
                c.RegisterMutationType<Mutation>();
            });

            // assert
            var query = schema.GetType<ObjectType>("Query");
            Assert.Collection(query.Fields
                .Where(t => !t.IsIntrospectionField)
                .OrderBy(t => t.Name),
                f => Assert.Equal(f.Name, "a"),
                f => Assert.Equal(f.Name, "c"),
                f => Assert.Equal(f.Name, "d"),
                f => Assert.Equal(f.Name, "f"));
        }

        [Fact]
        public void DiscoverMutationResolvers()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<Query>();
                c.RegisterMutationType<Mutation>();
            });

            // assert
            var query = schema.GetType<ObjectType>("Mutation");
            Assert.Collection(query.Fields
                .Where(t => !t.IsIntrospectionField)
                .OrderBy(t => t.Name),
                f => Assert.Equal(f.Name, "b"),
                f => Assert.Equal(f.Name, "c"),
                f => Assert.Equal(f.Name, "e"),
                f => Assert.Equal(f.Name, "f"));
        }

        [Fact]
        public void DiscoverQueryResolversByClrType()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterType<QueryResolvers3>();
                c.RegisterQueryType<Query>();
                c.RegisterMutationType<Mutation>();
            });

            // assert
            var query = schema.GetType<ObjectType>("Query");
            Assert.Collection(query.Fields
                .Where(t => !t.IsIntrospectionField)
                .OrderBy(t => t.Name),
                f => Assert.Equal(f.Name, "a"),
                f => Assert.Equal(f.Name, "c"),
                f => Assert.Equal(f.Name, "d"),
                f => Assert.Equal(f.Name, "f"),
                f => Assert.Equal(f.Name, "g"));
        }

        [Fact]
        public void DiscoverQueryResolversByName()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterType<QueryResolvers4>();
                c.RegisterQueryType<Query>();
                c.RegisterMutationType<Mutation>();
            });

            // assert
            var query = schema.GetType<ObjectType>("Query");
            Assert.Collection(query.Fields
                .Where(t => !t.IsIntrospectionField)
                .OrderBy(t => t.Name),
                f => Assert.Equal(f.Name, "a"),
                f => Assert.Equal(f.Name, "c"),
                f => Assert.Equal(f.Name, "d"),
                f => Assert.Equal(f.Name, "f"),
                f => Assert.Equal(f.Name, "h"));
        }

        [Fact]
        public void DiscoverQueryResolversByObjectType()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterType<QueryResolvers5>();
                c.RegisterQueryType<Query>();
                c.RegisterMutationType<Mutation>();
            });

            // assert
            var query = schema.GetType<ObjectType>("Query");
            Assert.Collection(query.Fields
                .Where(t => !t.IsIntrospectionField)
                .OrderBy(t => t.Name),
                f => Assert.Equal(f.Name, "a"),
                f => Assert.Equal(f.Name, "c"),
                f => Assert.Equal(f.Name, "d"),
                f => Assert.Equal(f.Name, "f"),
                f => Assert.Equal(f.Name, "i"));
        }

        public class QueryType
            : ObjectType<Query>
        {
        }

        [GraphQLResolver(typeof(QueryResolvers1), typeof(QueryResolvers2))]
        public class Query
        {
            public string A { get; }
        }

        [GraphQLResolver(typeof(QueryResolvers1), typeof(QueryResolvers2))]
        public class Mutation
                    : IMutation
        {
            public string B { get; }
        }

        public interface IMutation
        {
            string B { get; }
        }

        public class QueryResolvers1
        {
            public string C { get; set; }

            public string GetD([Parent]Query query)
            {
                return query.A;
            }

            public string GetE([Parent]IMutation mutation)
            {
                return mutation.B;
            }
        }

        public class QueryResolvers2
        {
            public string F { get; set; }
        }

        [GraphQLResolverOf(typeof(Query))]
        public class QueryResolvers3
        {
            public string G { get; set; }
        }

        [GraphQLResolverOf("Query")]
        public class QueryResolvers4
        {
            public string H { get; set; }
        }

        [GraphQLResolverOf(typeof(QueryType))]
        public class QueryResolvers5
        {
            public string I { get; set; }
        }
    }
}
