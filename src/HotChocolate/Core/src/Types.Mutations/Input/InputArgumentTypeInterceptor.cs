using System.Linq;

#nullable enable

namespace HotChocolate.Types;

internal class InputArgumentTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (definition is not ObjectTypeDefinition def)
        {
            return;
        }

        foreach (ObjectFieldDefinition field in def.Fields)
        {
            // get the graphql argument name that was specified on the member

            string? fieldInputName = null;
            string? fieldInputTypeName = null;
            if (field.ContextData.TryGetValue(InputContextData.Input, out var contextObj) &&
                contextObj is InputContextData context)
            {
                fieldInputName = context.ArgumentName;
                fieldInputTypeName = context.TypeName;
            }

            Dictionary<string, ArgumentReference>? arguments = null;
            for (var i = field.Arguments.Count - 1; i >= 0; i--)
            {
                ArgumentDefinition argument = field.Arguments[i];

                // get the graphql argument name that was specified on the parameter
                string? argumentInputName = null;
                string? argumentInputTypeName = null;

                if (argument.ContextData.TryGetValue(InputContextData.Input, out contextObj) &&
                    contextObj is InputContextData argumentContext)
                {
                    argumentInputName = argumentContext.ArgumentName;
                    argumentInputTypeName = argumentContext.TypeName;
                }

                // if the argument name of the parameter is null, assign the parameter name of
                // the field
                argumentInputName ??= fieldInputName;

                if (argumentInputName is not null)
                {
                    arguments ??= new Dictionary<string, ArgumentReference>();
                    if (!arguments.TryGetValue(argumentInputName, out ArgumentReference? reference))
                    {
                        reference = new ArgumentReference(
                            fieldInputTypeName,
                            argumentInputTypeName);
                        arguments[argumentInputName] = reference;
                    }

                    if (argumentInputTypeName is not null &&
                        argumentInputTypeName != reference.TypeNameOnArgument)
                    {
                        if (reference.TypeNameOnArgument is not null)
                        {
                            throw ThrowHelper.ArgumentTypeNameMissMatch(def,
                                argumentInputName,
                                field,
                                reference.TypeNameOnArgument,
                                argumentInputTypeName);
                        }

                        reference.TypeNameOnArgument = argumentInputTypeName;
                    }

                    reference.ArgumentDefinitions.Add(argument);
                    field.Arguments.RemoveAt(i);
                }
            }

            if (arguments is not { Count: > 0 })
            {
                continue;
            }

            foreach (KeyValuePair<string, ArgumentReference> argument in arguments)
            {
                if (field.Type is null)
                {
                    continue;
                }

                NameString typeName =
                    argument.Value.TypeNameOnArgument ??
                    argument.Value.TypeName ??
                    field.Name.ToTypeName(argument.Key, "Input");

                DependantFactoryTypeReference typeReference =
                    new(typeName, field.Type, CreateType, TypeContext.Output);

                field.Arguments.Add(new ArgumentDefinition(argument.Key, "", typeReference));

                TypeSystemObjectBase CreateType(IDescriptorContext _) =>
                    new InputObjectType(x =>
                    {
                        x.Name(typeName);
                        foreach (ArgumentDefinition argumentDefinition in
                            argument.Value.ArgumentDefinitions)
                        {
                            MergeFieldWithArgument(
                                x.Field(argumentDefinition.Name),
                                argumentDefinition);
                        }
                    });
            }
        }
    }

    private static void MergeFieldWithArgument(
        IInputFieldDescriptor descriptor,
        ArgumentDefinition argumentDefinition)
    {
        InputFieldDefinition definition = descriptor.Extend().Definition;

        definition.Type = argumentDefinition.Type;
        definition.Description = argumentDefinition.Description;
        definition.DefaultValue = argumentDefinition.DefaultValue;
        definition.Ignore = argumentDefinition.Ignore;
        definition.RuntimeDefaultValue = argumentDefinition.RuntimeDefaultValue;
        definition.RuntimeType = argumentDefinition.Parameter?.ParameterType;
        definition.ContextData.AddRange(argumentDefinition.ContextData);
        definition.Formatters.AddRange(argumentDefinition.Formatters);

        if (argumentDefinition.HasConfigurations)
        {
            definition.Configurations.AddRange(
                argumentDefinition.Configurations.Select(x => x.Copy(definition)));
        }

        if (argumentDefinition.HasDependencies)
        {
            definition.Dependencies.AddRange(argumentDefinition.Dependencies);
        }

        if (argumentDefinition.HasDirectives)
        {
            definition.Directives.AddRange(argumentDefinition.Directives);
        }
    }

    private class ArgumentReference
    {
        public ArgumentReference(string? typeName, string? typeNameOnArgument)
        {
            TypeName = typeName;
            TypeNameOnArgument = typeNameOnArgument;
        }

        public List<ArgumentDefinition> ArgumentDefinitions { get; } = new();

        public string? TypeName { get; }

        public string? TypeNameOnArgument { get; set; }
    }
}
