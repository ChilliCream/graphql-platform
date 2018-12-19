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
                f => Assert.Equal("a", f.Name),
                f => Assert.Equal("c", f.Name),
                f => Assert.Equal("d", f.Name),
                f => Assert.Equal("f", f.Name));
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
                f => Assert.Equal("b", f.Name),
                f => Assert.Equal("c", f.Name),
                f => Assert.Equal("e", f.Name),
                f => Assert.Equal("f", f.Name));
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
                f => Assert.Equal("a", f.Name),
                f => Assert.Equal("c", f.Name),
                f => Assert.Equal("d", f.Name),
                f => Assert.Equal("f", f.Name),
                f => Assert.Equal("g", f.Name));
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
                f => Assert.Equal("a", f.Name),
                f => Assert.Equal("c", f.Name),
                f => Assert.Equal("d", f.Name),
                f => Assert.Equal("f", f.Name),
                f => Assert.Equal("h", f.Name));
        }

        [Fact]
        public void DiscoverQueryResolversByObjectType()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterType<QueryResolvers5>();
                c.RegisterQueryType<QueryType>();
                c.RegisterMutationType<Mutation>();
            });

            // assert
            var query = schema.GetType<ObjectType>("Query");
            Assert.Collection(query.Fields
                .Where(t => !t.IsIntrospectionField)
                .OrderBy(t => t.Name),
                f => Assert.Equal("a", f.Name),
                f => Assert.Equal("c", f.Name),
                f => Assert.Equal("d", f.Name),
                f => Assert.Equal("f", f.Name),
                f => Assert.Equal("i", f.Name));
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
