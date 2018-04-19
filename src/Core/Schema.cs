using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    public class Schema
        : ISchema
    {
        public IType GetType(string name)
        {
            throw new NotImplementedException();
        }

        public T GetType<T>(string name) where T : IType
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IType> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public static Schema Create(
            DocumentNode schemaDocument,
            Action<ISchemaConfiguration> configure)
        {



            throw new NotImplementedException();
        }
    }

    internal class SchemaConfiguration
        : ISchemaConfiguration
    {
        private readonly List<FieldResolver> _resolverRegistry =
            new List<FieldResolver>();
        private readonly List<FieldResolverDescriptor> _resolverDescriptors =
            new List<FieldResolverDescriptor>();

        public ISchemaConfiguration Name<TObjectType>(string typeName)
        {
            throw new NotImplementedException();
        }

        public ISchemaConfiguration Name<TObjectType>(string typeName, params Action<IFluentFieldMapping<TObjectType>>[] fieldMapping)
        {
            throw new NotImplementedException();
        }

        public ISchemaConfiguration Name<TObjectType>(params Action<IFluentFieldMapping<TObjectType>>[] fieldMapping)
        {
            throw new NotImplementedException();
        }

        public ISchemaConfiguration Resolver(
            string typeName, string fieldName,
            FieldResolverDelegate fieldResolver)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            _resolverRegistry.Add(new FieldResolver(
                typeName, fieldName, fieldResolver));
            return this;
        }

        public ISchemaConfiguration Resolver<TResolver>()
        {
            return Resolver<TResolver>(typeof(TResolver).Name);
        }

        public ISchemaConfiguration Resolver<TResolver>(string typeName)
        {
            throw new NotImplementedException();
        }

        public ISchemaConfiguration Resolver<TResolver, TObjectType>()
        {
            return Resolver<TResolver, TObjectType>(typeof(TResolver).Name);
        }

        public ISchemaConfiguration Resolver<TResolver, TObjectType>(string typeName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<FieldResolver> CreateResolvers()
        {
            throw new NotImplementedException();
        }


    }


    internal class ReflectonHelper
    {

    }

    public interface ISchemaConfiguration
    {
        ISchemaConfiguration Resolver(string typeName, string fieldName, FieldResolverDelegate fieldResolver);
        ISchemaConfiguration Resolver<TResolver>();
        ISchemaConfiguration Resolver<TResolver>(string typeName);
        ISchemaConfiguration Resolver<TResolver, TObjectType>();
        ISchemaConfiguration Resolver<TResolver, TObjectType>(string typeName);

        ISchemaConfiguration Name<TObjectType>(string typeName);
        ISchemaConfiguration Name<TObjectType>(string typeName,
            params Action<IFluentFieldMapping<TObjectType>>[] fieldMapping);
        ISchemaConfiguration Name<TObjectType>(
            params Action<IFluentFieldMapping<TObjectType>>[] fieldMapping);

    }

    public interface IFluentFieldMapping<TObjectType>
        : IFluentFieldMapping
    {
        IFluentFieldMapping<TObjectType> Field<TPropertyType>(
            Expression<Func<TObjectType, TPropertyType>> field,
            string fieldName);

        new IFluentFieldMapping<TObjectType> Field(
            string propertyName,
            string fieldName);
    }

    public interface IFluentFieldMapping
    {
        IFluentFieldMapping Field(
            string propertyName,
            string fieldName);
    }

    public class Foo1
    {
        public void Bar()
        {
            Schema.Create(null, c =>
            {
                c.Resolver<Query>();
                c.Resolver("Query", "a", (context, cancellationToken) =>
                {
                    return Task.FromResult<object>(
                        context.Parent<Query>().A(context.Argument<string>("x")));
                });
                c.Resolver("Query", "b", (context, cancellationToken) =>
                {
                    return Task.FromResult<object>(
                        context.Parent<Query>().B(context.Argument<string>("x")));
                });


                string X = "type Y { x: String y: String}";


                c.Resolver<YResolver, Y>();
                c.Resolver<YResolverB, Y>();

                c.Name<Query>("YY",
                    t => t.Field(f => f.A(null), "a"),
                    t => t.Field(f => f.B(null), "a2"),
                    t => t.Field("z", "d"));

                c.Name<Y>("YY", t => t.Field(f => f.X, "hello"));
            });
        }
    }

    public class Query
    {
        public Z A(string x)
        {

            throw new NotImplementedException();
        }

        public string B(string x)
        {

            throw new NotImplementedException();
        }
    }

    public class Z
    {
        public Y Y { get; set; }
    }

    public class Y
    {
        public string X { get; set; }
    }

    public class YResolver
    {
        public Z GetX(Y y)
        {
            throw new NotImplementedException();
        }
    }

    public class YResolverB
    {
        public Z GetY(Y y)
        {
            throw new NotImplementedException();
        }
    }
}