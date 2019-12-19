using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public static class FieldDescriptorUtilities
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
            AddImplicitFields<TDescriptor, TMember, TField>(
                descriptor,
                descriptor.ClrType,
                createdFieldDefinition,
                fields,
                handledMembers);
        }

        public static void AddImplicitFields<TDescriptor, TMember, TField>(
            TDescriptor descriptor,
            Type fieldBindingType,
            Func<TMember, TField> createdFieldDefinition,
            IDictionary<NameString, TField> fields,
            ISet<TMember> handledMembers)
            where TDescriptor : IHasDescriptorContext
            where TMember : MemberInfo
            where TField : FieldDefinitionBase
        {
            if (fieldBindingType != typeof(object))
            {
                foreach (TMember member in descriptor.Context.Inspector
                    .GetMembers(fieldBindingType)
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

        public static void DiscoverArguments(
            IDescriptorContext context,
            ICollection<ArgumentDefinition> arguments,
            MemberInfo member)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (member is MethodInfo method)
            {
                var processed = new HashSet<NameString>(
                    arguments.Select(t => t.Name));

                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    if (IsArgumentType(method, parameter))
                    {
                        ArgumentDefinition argumentDefinition =
                            ArgumentDescriptor
                                .New(context, parameter)
                                .CreateDefinition();

                        if (processed.Add(argumentDefinition.Name))
                        {
                            arguments.Add(argumentDefinition);
                        }
                    }
                }
            }
        }

        private static bool IsArgumentType(
            MemberInfo member,
            ParameterInfo parameter)
        {
            return ArgumentHelper
                .LookupKind(parameter, member.ReflectedType) ==
                    ArgumentKind.Argument;
        }
    }
}
