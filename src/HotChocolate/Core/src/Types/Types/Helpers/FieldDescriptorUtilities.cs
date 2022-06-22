using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Helpers;

public static class FieldDescriptorUtilities
{
    private static HashSet<NameString>? _names = new();

    public static void AddExplicitFields<TMember, TField>(
        IEnumerable<TField> fieldDefinitions,
        Func<TField, TMember?> resolveMember,
        IDictionary<NameString, TField> fields,
        ISet<TMember> handledMembers)
        where TMember : MemberInfo
        where TField : FieldDefinitionBase
    {
        foreach (var fieldDefinition in fieldDefinitions)
        {
            if (!fieldDefinition.Ignore)
            {
                fields[fieldDefinition.Name] = fieldDefinition;
            }

            var member = resolveMember(fieldDefinition);
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
        AddImplicitFields(
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
        Func<IReadOnlyList<TMember>, TMember, bool>? include = null,
        bool includeIgnoredMembers = false)
        where TDescriptor : IHasDescriptorContext
        where TMember : MemberInfo
        where TField : FieldDefinitionBase
    {
        if (fieldBindingType != typeof(object))
        {
            var members = descriptor.Context.TypeInspector
                .GetMembers(fieldBindingType, includeIgnoredMembers)
                .OfType<TMember>()
                .ToList();

            foreach (var member in members)
            {
                if (include?.Invoke(members, member) ?? true)
                {
                    var fieldDefinition = createdFieldDefinition(member);

                    if (!handledMembers.Contains(member) &&
                        !fields.ContainsKey(fieldDefinition.Name) &&
                        (includeIgnoredMembers || !fieldDefinition.Ignore))
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
            var processedNames = Interlocked.Exchange(ref _names, null) ?? new();

            try
            {
                foreach (var argument in arguments)
                {
                    processedNames.Add(argument.Name);
                }

                foreach (var parameter in
                    context.ResolverCompiler.GetArgumentParameters(method.GetParameters()))
                {
                    var argumentDefinition =
                        ArgumentDescriptor
                            .New(context, parameter)
                            .CreateDefinition();

                    if (processedNames.Add(argumentDefinition.Name))
                    {
                        arguments.Add(argumentDefinition);
                    }
                }
            }
            finally
            {
                processedNames.Clear();

                Interlocked.CompareExchange(ref _names, processedNames, null);
            }
        }
    }
}
