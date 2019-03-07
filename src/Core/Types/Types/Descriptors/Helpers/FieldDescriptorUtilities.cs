using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    internal static class FieldDescriptorUtilities
    {
        public static void AddExplicitFields<TMember, TField>(
            IEnumerable<TField> fieldDefinitions,
            Func<TField, TMember> resolveMember,
            IDictionary<NameString, TField> fields,
            ISet<TMember> handledMembers)
            where TMember : MemberInfo
            where TField : FieldDefinitionBase
        {
            foreach (TField fieldDefinition in fieldDefinitions)
            {
                if (!fieldDefinition.Ignore)
                {
                    fields[fieldDefinition.Name] = fieldDefinition;
                }

                TMember member = resolveMember(fieldDefinition);
                if (member != null)
                {
                    handledMembers.Add(member);
                }
            }
        }

        public static void AddImplicitFields<TDescriptor, TMember, TField>(
            TDescriptor descriptor,
            Func<TMember, TField> createdFieldDefinition,
            IDictionary<NameString, TField> fields,
            ISet<TMember> handledMembers)
            where TDescriptor : IHasClrType, IHasDescriptorContext
            where TMember : MemberInfo
            where TField : FieldDefinitionBase
        {
            if (descriptor.ClrType != typeof(object))
            {
                foreach (TMember member in descriptor.Context.Inspector
                    .GetMembers(descriptor.ClrType)
                    .OfType<TMember>())
                {
                    TField fieldDefinition = createdFieldDefinition(member);

                    if (!handledMembers.Contains(member)
                        && !fields.ContainsKey(fieldDefinition.Name))
                    {
                        handledMembers.Add(member);
                        fields[fieldDefinition.Name] = fieldDefinition;
                    }
                }
            }
        }




        internal static void DiscoverArguments(
            ICollection<ArgumentDefinition> arguments,
            MemberInfo member)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            var processed = new HashSet<NameString>();

            foreach (ArgumentDescription description in arguments)
            {
                processed.Add(description.Name);
            }

            if (member is MethodInfo method)
            {
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    string argumentName = parameter.GetGraphQLName();
                    if (IsArgumentType(method, parameter)
                        && processed.Add(argumentName))
                    {
                        var argumentDescriptor = new ArgumentDescriptor(
                            argumentName, parameter.ParameterType);

                        argumentDescriptor.Description(
                            parameter.GetGraphQLDescription());

                        arguments.Add(argumentDescriptor.CreateDescription());
                    }
                }
            }
        }

        private static bool IsArgumentType(
            MemberInfo member,
            ParameterInfo parameter)
        {
            return (ArgumentHelper
                .LookupKind(parameter, member.ReflectedType) ==
                    ArgumentKind.Argument);
        }
    }
}
