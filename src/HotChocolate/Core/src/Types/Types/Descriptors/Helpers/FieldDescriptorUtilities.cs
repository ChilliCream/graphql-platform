using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public static class FieldDescriptorUtilities
    {
        public static void AddExplicitFields<TMember, TField>(
            IEnumerable<TField> fieldDefinitions,
            Func<TField, TMember?> resolveMember,
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

                TMember? member = resolveMember(fieldDefinition);
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
            where TDescriptor : IHasRuntimeType, IHasDescriptorContext
            where TMember : MemberInfo
            where TField : FieldDefinitionBase
        {
            AddImplicitFields<TDescriptor, TMember, TField>(
                descriptor,
                descriptor.RuntimeType,
                createdFieldDefinition,
                fields,
                handledMembers);
        }

        public static void AddImplicitFields<TDescriptor, TMember, TField>(
            TDescriptor descriptor,
            Type fieldBindingType,
            Func<TMember, TField> createdFieldDefinition,
            IDictionary<NameString, TField> fields,
            ISet<TMember> handledMembers,
            Func<IReadOnlyList<TMember>, TMember, bool>? include = null)
            where TDescriptor : IHasDescriptorContext
            where TMember : MemberInfo
            where TField : FieldDefinitionBase
        {
            if (fieldBindingType != typeof(object))
            {
                List<TMember> members = descriptor.Context.TypeInspector
                    .GetMembers(fieldBindingType)
                    .OfType<TMember>()
                    .ToList();

                foreach (TMember member in members)
                {
                    if (include?.Invoke(members, member) ?? true)
                    {
                        TField fieldDefinition = createdFieldDefinition(member);

                        if (!handledMembers.Contains(member) &&
                            !fields.ContainsKey(fieldDefinition.Name) &&
                            !fieldDefinition.Ignore)
                        {
                            handledMembers.Add(member);
                            fields[fieldDefinition.Name] = fieldDefinition;
                        }
                    }
                }
            }
        }

        public static void DiscoverArguments(
            IDescriptorContext context,
            ICollection<ArgumentDefinition> arguments,
            MemberInfo? member)
        {
            if (arguments is null)
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
                    else if (IsSpreadableArgumentType(method, parameter))
                    {
                        // foreach member of the object/class
                        // add an argument
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

        private static bool IsSpreadableArgumentType(
            MemberInfo member,
            ParameterInfo parameter)
        {
            return ArgumentHelper
                .LookupKind(parameter, member.ReflectedType) ==
                    ArgumentKind.SpreadableArgument;
        }
    }
}
