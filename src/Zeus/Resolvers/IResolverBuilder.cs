using System;
using System.Threading;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    public interface IResolverBuilder
    {
        IResolverBuilder Add(
            string typeName, string fieldName,
            ResolverFactory resolverFactory);

        IResolverCollection Build();
    }
/* 

    public interface IResolverBuilder2
    {
        IResolverBuilder2 Add(IResolverBuilderTask task);
    }

    public interface IResolverBuilderTask
    {
        ResolverFactory CreateFactory(SchemaDocument schema);
    }

    public class FieldResolverBuilderTask
        : IResolverBuilderTask
    {
        public FieldResolverBuilderTask(string typeName, string fieldName,
            ResolverFactory factory)
            : this(typeName, fieldName, schema => factory)
        {
        }

        public FieldResolverBuilderTask(string typeName, string fieldName,
            Func<SchemaDocument, ResolverFactory> factory)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("message", nameof(typeName));
            }

            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException("message", nameof(fieldName));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

        
        }

        public string TypeName { get; }
        public string FieldName { get; }

        public ResolverFactory CreateFactory(SchemaDocument schema)
        {
            
        }
    }



     */
}