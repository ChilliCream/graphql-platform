using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Helpers;

public static class FieldDescriptorUtilities
{
    public static void AddExplicitFields<TMember, TField>(
        IEnumerable<TField> fieldDefinitions,
        Func<TField, TMember?> resolveMember,
        IDictionary<string, TField> fields,
        ISet<TMember> handledMembers)
        where TMember : MemberInfo
        where TField : FieldDefinitionBase
    {
        foreach (var fieldDefinition in fieldDefinitions)
        {
            if (!fieldDefinition.Ignore && !string.IsNullOrEmpty(fieldDefinition.Name))
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
        IDictionary<string, TField> fields,
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
        IDictionary<string, TField> fields,
        ISet<TMember> handledMembers,
        Func<IReadOnlyList<TMember>, TMember, bool>? include = null,
        bool includeIgnoredMembers = false)
        where TDescriptor : IHasDescriptorContext
        where TMember : MemberInfo
        where TField : FieldDefinitionBase
    {
        if (fieldBindingType != typeof(object))
        {
            var inspector = descriptor.Context.TypeInspector;
            var members = new List<TMember>();

            foreach (var member in inspector.GetMembers(fieldBindingType, includeIgnoredMembers))
            {
                if (member is TMember m)
                {
                    members.Add(m);
                }
            }

            foreach (var member in members)
            {
                if (include?.Invoke(members, member) ?? true)
                {
                    var fieldDefinition = createdFieldDefinition(member);

                    if (!string.IsNullOrEmpty(fieldDefinition.Name) &&
                        !handledMembers.Contains(member) &&
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
        MemberInfo? member,
        ParameterInfo[] parameters,
        IReadOnlyList<IParameterExpressionBuilder>? parameterExpressionBuilders)
    {
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        if (member is MethodInfo)
        {
            var processedNames = TypeMemHelper.RentNameSet();

            try
            {
                foreach (var argument in arguments)
                {
                    if (!string.IsNullOrEmpty(argument.Name))
                    {
                        processedNames.Add(argument.Name);
                    }
                }

                foreach (var parameter in
                    context.ResolverCompiler.GetArgumentParameters(
                        parameters,
                        parameterExpressionBuilders))
                {
                    var argumentDefinition =
                        ArgumentDescriptor
                            .New(context, parameter)
                            .CreateDefinition();

                    if (!string.IsNullOrEmpty(argumentDefinition.Name) &&
                        processedNames.Add(argumentDefinition.Name))
                    {
                        arguments.Add(argumentDefinition);
                    }
                }
            }
            finally
            {
                TypeMemHelper.Return(processedNames);
            }
        }
    }
}
