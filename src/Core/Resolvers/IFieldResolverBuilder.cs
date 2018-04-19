using System;
using System.Collections.Generic;
using System.Threading;

namespace HotChocolate.Resolvers
{
    public interface IFieldResolverBuilder
    {
        IEnumerable<FieldResolver> Build(
            IEnumerable<FieldResolverDescriptor> fieldResolverDescriptors);
    }

    public class FieldResolverSourceCodeGenerator
    {
        public string Generate(IEnumerable<FieldResolverDescriptor> fieldResolverDescriptors)
        {
            throw new NotImplementedException();
        }




        

    }
}