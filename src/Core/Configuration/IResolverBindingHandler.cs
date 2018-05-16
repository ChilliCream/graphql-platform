using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal interface IResolverBindingHandler
    {
        IEnumerable<FieldResolver> ApplyBinding(
            ResolverBindingInfo resolverBindingInfo);
    }

    internal class ResolverCollectionBindingHandler
        : IResolverBindingHandler
    {
        public IEnumerable<FieldResolver> ApplyBinding(ResolverBindingInfo resolverBindingInfo)
        {


        }

        public void Commit(SchemaContext context)
        {
            foreach (INamedType type in _registeredTypes.Values)
            {
                context.RegisterType(type);
            }

            FieldResolverDescriptorFactory fieldResolverDescriptorFactory =
                new FieldResolverDescriptorFactory(
                    _resolverObjectTypeMapping,
                    GetObjectTypeName, GetFieldName);
            FieldResolverBuilder fieldResolverBuilder = new FieldResolverBuilder();
            IEnumerable<FieldResolver> fieldResolvers = fieldResolverBuilder
                .Build(GetBestMatchingFieldResolvers(
                    context, fieldResolverDescriptorFactory.Create()));

        }

        private IEnumerable<FieldResolverDescriptor> GetBestMatchingFieldResolvers(
            SchemaContext context,
            IEnumerable<FieldResolverDescriptor> resolverDescriptors)
        {
            foreach (var resolverGroup in resolverDescriptors.GroupBy(r => r.Field))
            {
                FieldReference fieldReference = resolverGroup.Key;
                if (context.TryGetOutputType<ObjectType>(
                        fieldReference.TypeName, out ObjectType type)
                    && type.Fields.TryGetValue(
                        fieldReference.FieldName, out Field field))
                {
                    foreach (FieldResolverDescriptor resolverDescriptor in
                        resolverGroup.OrderByDescending(t => t.ArgumentCount()))
                    {
                        if (AllArgumentsMatch(field, resolverDescriptor))
                        {
                            yield return resolverDescriptor;
                            break;
                        }
                    }
                }
            }
        }

        private bool AllArgumentsMatch(Field field, FieldResolverDescriptor resolverDescriptor)
        {
            foreach (FieldResolverArgumentDescriptor argumentDescriptor in
                resolverDescriptor.Arguments())
            {
                if (!field.Arguments.ContainsKey(argumentDescriptor.Name))
                {
                    return false;
                }

                // TODO : Check that argument types are compatible.
                /*
                if (field.Arguments.TryGetValue(
                    argumentDescriptor.Name, out InputField inputField))
                {

                }
                 */
            }
            return true;
        }

        private string GetObjectTypeName(Type objectType)
        {
            if (_typeAliases.TryGetValue(objectType, out string name))
            {
                return name;
            }
            return objectType.Name;
        }

        private string GetFieldName(MemberResolverInfo resolverInfo, Type resolverType)
        {
            string name = string.IsNullOrEmpty(resolverInfo.Alias)
                ? ReflectionHelper.AdjustCasing(resolverInfo.Member.Name)
                : resolverInfo.Alias;

            if (_fieldAliases.TryGetValue(resolverType,
                out Dictionary<MemberInfo, string> mappings)
                && mappings.TryGetValue(resolverInfo.Member, out string alias))
            {
                name = alias;
            }

            return name;
        }
    }
}
