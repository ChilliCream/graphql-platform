using System.Reflection;
using System;
using HotChocolate.Types.Descriptors.Definitions;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Types.Descriptors
{
    public interface IDescriptorContext
    {
        INamingConventions Naming { get; }

        IObjectInspector Inspector { get; }
    }


    public interface INamingConventions
    {
        NameString GetTypeName(Type type);

        string GetTypeDescription(Type type);

        NameString GetMemberName(MemberInfo member);

        string GetMemberDescription(MemberInfo member);
    }

    public interface IObjectInspector
    {
        IEnumerable<Type> GetResolverTypes(Type sourceType);
        IEnumerable<MemberInfo> GetMembers(Type type);
        ITypeReference GetReturnType(MemberInfo member);
    }

    public class DefaultObjectInspector
        : IObjectInspector
    {
        public IEnumerable<MemberInfo> GetMembers(Type type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Type> GetResolverTypes(Type sourceType)
        {
            if (sourceType.IsDefined(typeof(GraphQLResolverAttribute)))
            {
                return sourceType
                    .GetCustomAttributes(typeof(GraphQLResolverAttribute))
                    .OfType<GraphQLResolverAttribute>()
                    .SelectMany(attr => attr.ResolverTypes);
            }
            return Enumerable.Empty<Type>();
        }

        public ITypeReference GetReturnType(MemberInfo member)
        {
            throw new NotImplementedException();
        }
    }
}
