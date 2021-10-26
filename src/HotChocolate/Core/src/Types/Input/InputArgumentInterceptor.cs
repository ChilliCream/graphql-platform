using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Input
{
    internal class InputArgumentInterceptor : TypeInterceptor
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

                string? fieldInputName =
                    field.ContextData.TryGetValue(InputContextData.Input, out object strObj)
                        ? strObj as string
                        : null;

                Dictionary<string, List<ArgumentDefinition>>? arguments = null;
                for (var i = field.Arguments.Count - 1; i >= 0; i--)
                {
                    ArgumentDefinition argument = field.Arguments[i];

                    // get the graphql argument name that was specified on the parameter
                    string? argumentInputName =
                        argument.ContextData.TryGetValue(InputContextData.Input, out strObj)
                            ? strObj as string
                            : null;

                    // if the argument name of the parameter is null, assign the parameter name of
                    // the field
                    argumentInputName ??= fieldInputName;

                    if (argumentInputName is not null)
                    {
                        arguments ??= new Dictionary<string, List<ArgumentDefinition>>();
                        if (!arguments.TryGetValue(argumentInputName, out var list))
                        {
                            list = new List<ArgumentDefinition>();
                            arguments[argumentInputName] = list;
                        }

                        list.Add(argument);
                        field.Arguments.RemoveAt(i);
                    }
                }

                if (arguments is not { Count: >0 })
                {
                    continue;
                }

                foreach (var argument in arguments)
                {
                    if (field.Type is null)
                    {
                        continue;
                    }

                    NameString typeName = field.Name.ToTypeName(argument.Key, "Input");

                    DependantFactoryTypeReference typeReference =
                        new(typeName, field.Type, CreateType, TypeContext.Output);

                    field.Arguments.Add(new ArgumentDefinition(argument.Key, "", typeReference));

                    TypeSystemObjectBase CreateType(IDescriptorContext _) =>
                        new InputObjectType(x =>
                        {
                            x.Name(typeName);
                            foreach (var argumentDefinition in argument.Value)
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

            definition.ContextData.AddRange(argumentDefinition.ContextData);
            definition.Formatters.AddRange(argumentDefinition.Formatters);

            if (argumentDefinition.HasConfigurations)
            {
                definition.Configurations.AddRange(argumentDefinition.Configurations);
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
    }
}
