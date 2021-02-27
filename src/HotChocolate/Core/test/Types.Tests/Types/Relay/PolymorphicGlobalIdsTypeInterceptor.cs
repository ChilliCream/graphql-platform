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
    internal sealed class PolymorphicGlobalIdsTypeInterceptor : TypeInterceptor
    {
        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is InputObjectType inputObjectType &&
                definition is InputObjectTypeDefinition inputObjectTypeDefinition)
            {
                foreach (InputFieldDefinition inputFieldDefinition in inputObjectTypeDefinition.Fields)
                {
                    (string NodeTypeName, Type IdRuntimeType)? idInfo =
                        GetIdInfo(completionContext, inputFieldDefinition);
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
            // TODO: Object types that have fields with args...

            base.OnBeforeCompleteType(completionContext, definition, contextData);
        }

        private static void InsertFormatter(
            ITypeCompletionContext completionContext,
            ArgumentDefinition definition,
            NameString typeName,
            Type idRuntimeType)
        {
            var formatter = new PolymorphicGlobalIdInputValueFormatter(
                typeName,
                idRuntimeType,
                GetIdSerializer(completionContext));

            IInputValueFormatter? defaultFormatter = definition.Formatters
                .FirstOrDefault(f => f is GlobalIdInputValueFormatter);

            if (defaultFormatter == null)
            {
                definition.Formatters.Add(formatter);
            }
            else
            {
                definition.Formatters.Insert(
                    definition.Formatters.IndexOf(defaultFormatter) - 1,
                    formatter);
            }
        }

        private static (string NodeTypeName, Type IdRuntimeType)? GetIdInfo(
            ITypeCompletionContext completionContext,
            ArgumentDefinition definition)
        {
            ITypeInspector typeInspector = completionContext.TypeInspector;
            IDAttribute? idAttribute;
            IExtendedType? idType;

            if (definition is InputFieldDefinition inputField)
            {
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
                throw new NotImplementedException("TODO: Equivalent for IDAttribute here?");
                idType = typeReference.Type;
            }
            else
            {
                throw new SchemaException(SchemaErrorBuilder.New()
                    .SetMessage("Unable to resolve type from field `{0}`.", definition.Name)
                    .SetTypeSystemObject(completionContext.Type)
                    .Build());
            }

            Type idRuntimeType = idType.ElementType?.Source ?? idType.Source;

            return (idAttribute.TypeName, idRuntimeType);
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
