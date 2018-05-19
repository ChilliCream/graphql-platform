using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Abstractions;
using HotChocolate.Resolvers;

namespace HotChocolate.Introspection
{
    internal static class IntrospectionSchemaExtensions
    {
        // naming
        public static SchemaDocument WithIntrospectionSchema(this SchemaDocument originalSchema)
        {
            if (originalSchema == null)
            {
                throw new ArgumentNullException(nameof(originalSchema));
            }

            List<ITypeDefinition> typeDefinitions = new List<ITypeDefinition>(originalSchema);
            if (typeDefinitions.Any(t => t.Name == "__schema"))
            {
                throw new ArgumentException("The specified schema already contains introspection schema definitions.");
            }

            ObjectTypeDefinition[] objectTypeDefinitions = typeDefinitions.OfType<ObjectTypeDefinition>().ToArray();
            foreach (ObjectTypeDefinition originalTypeDefinition in objectTypeDefinitions)
            {
                IEnumerable<FieldDefinition> fields = originalTypeDefinition
                    .Fields.Values
                    .Concat(new[] { TypeName.FieldDefinition });

                if (originalSchema.QueryType == originalTypeDefinition)
                {
                    fields = fields.Concat(new[] {
                        new FieldDefinition("__schema", IntrospectionTypes.NonNullSchema, true),
                        new FieldDefinition("__type", IntrospectionTypes.NonNullType, true)
                    });
                }

                ObjectTypeDefinition newTypeDefinition = new ObjectTypeDefinition(
                    originalTypeDefinition.Name, fields, originalTypeDefinition.Interfaces);

                typeDefinitions.Remove(originalTypeDefinition);
                typeDefinitions.Add(newTypeDefinition);
            }

            typeDefinitions.AddRange(CreateSchemaDefinitions());

            return new SchemaDocument(typeDefinitions);
        }

        public static IEnumerable<ITypeDefinition> CreateSchemaDefinitions()
        {
            yield return CreateSchemaDefinition();
            yield return CreateTypeDefinition();
            yield return CreateFieldDefinition();
            yield return CreateInputValueDefinition();
            yield return CreateEnumValueDefinition();
            yield return CreateTypeKindDefinition();
            yield return CreateDirectiveDefinition();
            yield return CreateDirectiveLocation();
        }

        private static ObjectTypeDefinition CreateSchemaDefinition()
        {
            return new ObjectTypeDefinition("__Schema",
                new[] {
                    new FieldDefinition("types", new NonNullType(
                        new ListType(IntrospectionTypes.NonNullType))),
                    new FieldDefinition("queryType", IntrospectionTypes.NonNullType),
                    new FieldDefinition("mutationType", IntrospectionTypes.Type),
                    new FieldDefinition("directives", new NonNullType(
                        new ListType(IntrospectionTypes.NonNullDirective))),
                    new FieldDefinition("type", IntrospectionTypes.NonNullType),
                    new FieldDefinition("isDeprecated", NamedType.NonNullBoolean),
                    new FieldDefinition("deprecationReason", NamedType.String)
                });
        }

        private static ObjectTypeDefinition CreateTypeDefinition()
        {
            return new ObjectTypeDefinition("__Type",
                new[] {
                    new FieldDefinition("kind", IntrospectionTypes.NonNullTypeKind),
                    new FieldDefinition("name", NamedType.NonNullString),
                    new FieldDefinition("description", NamedType.String),
                    new FieldDefinition("fields", new ListType(IntrospectionTypes.NonNullField),
                        new[] {
                            new InputValueDefinition("includeDeprecated",
                                NamedType.Boolean, new BooleanValue(false))
                        }),
                    new FieldDefinition("interfaces", new ListType(IntrospectionTypes.NonNullType)),
                    new FieldDefinition("possibleTypes", new ListType(IntrospectionTypes.NonNullType)),
                    new FieldDefinition("enumValues", new ListType(IntrospectionTypes.NonNullEnumValue),
                        new[] {
                            new InputValueDefinition("includeDeprecated",
                                NamedType.Boolean, new BooleanValue(false))
                        }),
                    new FieldDefinition("inputFields", new ListType(IntrospectionTypes.NonNullInputValue)),
                    new FieldDefinition("ofType", IntrospectionTypes.Type)
                });
        }
        private static ObjectTypeDefinition CreateFieldDefinition()
        {
            return new ObjectTypeDefinition("__Field",
                new[] {
                    new FieldDefinition("name", NamedType.NonNullString),
                    new FieldDefinition("description", NamedType.String),
                    new FieldDefinition("args", new NonNullType(
                        new ListType(IntrospectionTypes.NonNullInputValue))),
                    new FieldDefinition("type", IntrospectionTypes.NonNullType),
                    new FieldDefinition("isDeprecated", NamedType.NonNullBoolean),
                    new FieldDefinition("deprecationReason", NamedType.String)
                });
        }

        private static ObjectTypeDefinition CreateInputValueDefinition()
        {
            return new ObjectTypeDefinition("__InputValue",
                new[] {
                    new FieldDefinition("name", NamedType.NonNullString),
                    new FieldDefinition("description", NamedType.String),
                    new FieldDefinition("type", IntrospectionTypes.NonNullType),
                    new FieldDefinition("defaultValue", NamedType.String)
                });
        }

        private static ObjectTypeDefinition CreateEnumValueDefinition()
        {
            return new ObjectTypeDefinition("__EnumValue",
                new[] {
                    new FieldDefinition("name", NamedType.NonNullString),
                    new FieldDefinition("description", NamedType.String),
                    new FieldDefinition("isDeprecated", NamedType.NonNullBoolean),
                    new FieldDefinition("defaultValue", NamedType.String)
                });
        }

        private static EnumTypeDefinition CreateTypeKindDefinition()
        {
            return new EnumTypeDefinition("__TypeKind", new[] {
                "SCALAR",
                "OBJECT",
                "INTERFACE",
                "UNION",
                "ENUM",
                "INPUT_OBJECT",
                "LIST",
                "NON_NULL",
            });
        }

        private static ObjectTypeDefinition CreateDirectiveDefinition()
        {
            return new ObjectTypeDefinition("__Directive",
                new[] {
                    new FieldDefinition("name", NamedType.NonNullString),
                    new FieldDefinition("description", NamedType.String),
                    new FieldDefinition("locations", new NonNullType(
                        new ListType(IntrospectionTypes.NonNullDirectiveLocation))),
                    new FieldDefinition("args", new NonNullType(
                        new ListType(IntrospectionTypes.NonNullInputValue)))
                });
        }

        private static EnumTypeDefinition CreateDirectiveLocation()
        {
            return new EnumTypeDefinition("__DirectiveLocation", new[] {
                "QUERY",
                "MUTATION",
                "FIELD",
                "FRAGMENT_DEFINITION",
                "FRAGMENT_SPREAD",
                "INLINE_FRAGMENT"
            });
        }
    }
}
