using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class TypeDefinitionExtensions
{
    extension(ITypeDefinition type)
    {
        /// <summary>
        /// Determines whether the type is an object, interface or union type that declares an
        /// <c>@context</c> directive with the specified name.
        /// </summary>
        public bool SetsContext(string contextName)
        {
            if (type is not (MutableObjectTypeDefinition
                or MutableInterfaceTypeDefinition
                or MutableUnionTypeDefinition))
            {
                return false;
            }

            foreach (var directive in type.Directives.AsEnumerable())
            {
                if (directive.Name == WellKnownDirectiveNames.Context
                    && directive.Arguments.TryGetValue(WellKnownArgumentNames.Name, out var value)
                    && value is StringValueNode { Value: var name }
                    && name == contextName)
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<MutableObjectTypeDefinition> GetPossibleTypes(
            string schemaName,
            MutableSchemaDefinition schema)
        {
            if (type.Kind is not TypeKind.Object and not TypeKind.Interface and not TypeKind.Union)
            {
                throw new ArgumentException(TypeDefinitionExtensions_TheSpecifiedTypeIsNotAnAbstractType);
            }

            return GetPossibleTypesInternal();

            IEnumerable<MutableObjectTypeDefinition> GetPossibleTypesInternal()
            {
                switch (type)
                {
                    case MutableObjectTypeDefinition objectType:
                        yield return objectType;
                        break;

                    case MutableInterfaceTypeDefinition interfaceType:
                        foreach (var possibleType in schema.Types)
                        {
                            if (possibleType is not MutableObjectTypeDefinition objectType)
                            {
                                continue;
                            }

                            var implementedInSchema = objectType.Directives[FusionImplements]
                                .Any(d => (string)d.Arguments[Schema].Value! == schemaName
                                    && (string)d.Arguments[Interface].Value! == interfaceType.Name);

                            if (implementedInSchema)
                            {
                                yield return objectType;
                            }
                        }

                        break;

                    case MutableUnionTypeDefinition unionType:
                        var memberTypeNamesInSchema =
                            unionType
                                .Directives[FusionUnionMember]
                                .Where(d => (string)d.Arguments[Schema].Value! == schemaName)
                                .Select(d => (string)d.Arguments[Member].Value!);

                        foreach (var memberType in memberTypeNamesInSchema)
                        {
                            yield return (MutableObjectTypeDefinition)schema.Types[memberType];
                        }

                        break;
                }
            }
        }
    }
}
