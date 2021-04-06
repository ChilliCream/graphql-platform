using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace HotChocolate.Types.Relay
{
    /// <summary>
    /// Note: This functionality is distributed as a nuget package called
    /// AutoGuru.HotChocolate.PolymorphicIds if you'd like to use it.
    /// </summary>
    internal sealed class PolymorphicGlobalIdsTypeInterceptor : TypeInterceptor
    {
        private const string StandardGlobalIdFormatterName = "GlobalIdInputValueFormatter";

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is InputObjectTypeDefinition inputObjectTypeDefinition)
            {
                foreach (InputFieldDefinition? inputFieldDefinition in inputObjectTypeDefinition.Fields)
                {
                    (string NodeTypeName, Type IdRuntimeType)? idInfo = GetIdInfo(completionContext, inputFieldDefinition);
                    if (idInfo != null)
                    {
                        InsertFormatter(
                            completionContext,
                            inputFieldDefinition,
                            idInfo.Value.NodeTypeName,
                            idInfo.Value.IdRuntimeType);
                    }
                }
            }
            else if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                var isQueryType = definition.Name == OperationTypeNames.Query;

                foreach (ObjectFieldDefinition? objectFieldDefinition in objectTypeDefinition.Fields)
                {
                    if (isQueryType && objectFieldDefinition.Name == "node")
                    {
                        continue;
                    }

                    foreach (ArgumentDefinition? argumentDefinition in objectFieldDefinition.Arguments)
                    {
                        (string NodeTypeName, Type IdRuntimeType)? idInfo = GetIdInfo(completionContext, argumentDefinition);
                        if (idInfo != null)
                        {
                            InsertFormatter(
                                completionContext,
                                argumentDefinition,
                                idInfo.Value.NodeTypeName,
                                idInfo.Value.IdRuntimeType);
                        }
                    }
                }
            }

            base.OnBeforeCompleteType(completionContext, definition, contextData);
        }

        private static void InsertFormatter(
            ITypeCompletionContext completionContext,
            ArgumentDefinition argumentDefinition,
            NameString typeName,
            Type idRuntimeType)
        {
            var formatter = new PolymorphicGlobalIdInputValueFormatter(
                typeName,
                idRuntimeType,
                GetIdSerializer(completionContext));

            IInputValueFormatter? defaultFormatter = argumentDefinition.Formatters
                .FirstOrDefault(f => f.GetType().Name == StandardGlobalIdFormatterName);

            if (defaultFormatter == null)
            {
                argumentDefinition.Formatters.Insert(0, formatter);
            }
            else
            {
                argumentDefinition.Formatters.Insert(
                    argumentDefinition.Formatters.IndexOf(defaultFormatter) - 1,
                    formatter);
            }
        }

        private static (string NodeTypeName, Type IdRuntimeType)? GetIdInfo(
            ITypeCompletionContext completionContext,
            ArgumentDefinition definition)
        {
            ITypeInspector? typeInspector = completionContext.TypeInspector;
            IDAttribute? idAttribute = null;
            IExtendedType? idType = null;

            if (definition is InputFieldDefinition inputField)
            {
                // UseSorting arg/s seems to come in here with a null Property
                if (inputField.Property == null)
                {
                    return null;
                }

                idAttribute = (IDAttribute?)inputField.Property
                   .GetCustomAttributes(inherit: true)
                   .SingleOrDefault(a => a is IDAttribute);
                if (idAttribute == null)
                {
                    return null;
                }

                idType = typeInspector.GetReturnType(inputField.Property, true);
            }
            else if (definition.Parameter is not null)
            {
                idAttribute = (IDAttribute?)definition.Parameter
                    .GetCustomAttributes(inherit: true)
                    .SingleOrDefault(a => a is IDAttribute);
                if (idAttribute == null)
                {
                    return null;
                }

                idType = typeInspector.GetArgumentType(definition.Parameter, true);
            }
            else if (definition.Type is ExtendedTypeReference typeReference)
            {
                if (typeReference.Type.Kind == ExtendedTypeKind.Schema)
                {
                    return null;
                }
            }

            if (idAttribute is null || idType is null)
            {
                throw new SchemaException(SchemaErrorBuilder.New()
                    .SetMessage("Unable to resolve type from field `{0}`.", definition.Name)
                    .SetTypeSystemObject(completionContext.Type)
                    .Build());
            }

            Type? idRuntimeType = idType.ElementType?.Source ?? idType.Source;
            string nodeTypeName = idAttribute?.TypeName.HasValue ?? false
                ? idAttribute.TypeName
                : completionContext.Type.Name;

            return (nodeTypeName, idRuntimeType);
        }

        private static IIdSerializer? _idSerializer;
        private static IIdSerializer GetIdSerializer(ITypeCompletionContext completionContext)
        {
            if (_idSerializer == null)
            {
                _idSerializer =
                    completionContext.Services.GetService<IIdSerializer>() ??
                    new IdSerializer();
            }
            return _idSerializer;
        }
    }
}
