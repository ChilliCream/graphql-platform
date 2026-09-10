using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.Fusion.ApolloFederation;

internal static class FederationV1SchemaAnalyzer
{
    private static readonly Dictionary<string, DirectiveSignature> s_signatures = new(
        StringComparer.Ordinal)
    {
        [FederationDirectiveNames.Key] = new(
            DirectiveLocation.Object | DirectiveLocation.Interface,
            WellKnownArgumentNames.Fields,
            "the required 'fields' argument only"),
        [FederationDirectiveNames.Extends] = new(
            DirectiveLocation.Object | DirectiveLocation.Interface,
            null,
            "no arguments"),
        [FederationDirectiveNames.External] = new(
            DirectiveLocation.FieldDefinition,
            null,
            "no arguments"),
        [FederationDirectiveNames.Requires] = new(
            DirectiveLocation.FieldDefinition,
            WellKnownArgumentNames.Fields,
            "the required 'fields' argument only"),
        [FederationDirectiveNames.Provides] = new(
            DirectiveLocation.FieldDefinition,
            WellKnownArgumentNames.Fields,
            "the required 'fields' argument only"),
        [FederationDirectiveNames.Tag] = new(
            DirectiveLocation.FieldDefinition
                | DirectiveLocation.Object
                | DirectiveLocation.Interface
                | DirectiveLocation.Union
                | DirectiveLocation.ArgumentDefinition
                | DirectiveLocation.Scalar
                | DirectiveLocation.Enum
                | DirectiveLocation.EnumValue
                | DirectiveLocation.InputObject
                | DirectiveLocation.InputFieldDefinition,
            "name",
            "the required 'name' argument only")
    };

    public static void Validate(
        MutableSchemaDefinition schema,
        ICompositionLog log)
    {
        ValidateProvider(schema, schema, DirectiveLocation.Schema, "schema", log);

        foreach (var type in schema.Types)
        {
            ValidateProvider(schema, type, GetTypeLocation(type), type.Name, log);

            switch (type)
            {
                case MutableComplexTypeDefinition complexType:
                    foreach (var field in complexType.Fields)
                    {
                        ValidateProvider(
                            schema,
                            field,
                            DirectiveLocation.FieldDefinition,
                            field.Coordinate.ToString(),
                            log);

                        foreach (var argument in field.Arguments)
                        {
                            ValidateProvider(
                                schema,
                                argument,
                                DirectiveLocation.ArgumentDefinition,
                                argument.Coordinate.ToString(),
                                log);
                        }
                    }

                    break;

                case MutableInputObjectTypeDefinition inputObjectType:
                    foreach (var field in inputObjectType.Fields)
                    {
                        ValidateProvider(
                            schema,
                            field,
                            DirectiveLocation.InputFieldDefinition,
                            field.Coordinate.ToString(),
                            log);
                    }

                    break;

                case MutableEnumTypeDefinition enumType:
                    foreach (var value in enumType.Values)
                    {
                        ValidateProvider(
                            schema,
                            value,
                            DirectiveLocation.EnumValue,
                            value.Coordinate.ToString(),
                            log);
                    }

                    break;
            }
        }

        foreach (var directiveDefinition in schema.DirectiveDefinitions)
        {
            ValidateProvider(
                schema,
                directiveDefinition,
                DirectiveLocation.DirectiveDefinition,
                $"@{directiveDefinition.Name}",
                log);

            foreach (var argument in directiveDefinition.Arguments)
            {
                ValidateProvider(
                    schema,
                    argument,
                    DirectiveLocation.ArgumentDefinition,
                    $"@{directiveDefinition.Name}({argument.Name}:)",
                    log);
            }
        }
    }

    private static void ValidateProvider(
        MutableSchemaDefinition schema,
        IDirectivesProvider provider,
        DirectiveLocation location,
        string member,
        ICompositionLog log)
    {
        foreach (var directive in provider.Directives)
        {
            if (!s_signatures.TryGetValue(directive.Name, out var signature))
            {
                continue;
            }

            if ((signature.Locations & location) == 0)
            {
                log.Write(
                    LogEntryBuilder.New()
                        .SetMessage(
                            FederationV1SchemaAnalyzer_InvalidDirectiveLocation,
                            schema.Name,
                            directive.Name,
                            member)
                        .SetCode(LogEntryCodes.FederationV1DirectiveNotSupported)
                        .SetSeverity(LogSeverity.Error)
                        .SetSchema(schema)
                        .SetTypeSystemMember(provider)
                        .Build());
            }

            var argumentsAreValid = signature.RequiredArgument is { } requiredArgument
                ? directive.Arguments.Count == 1
                    && directive.Arguments.ContainsName(requiredArgument)
                : directive.Arguments.Count == 0;

            if (!argumentsAreValid)
            {
                log.Write(
                    LogEntryBuilder.New()
                        .SetMessage(
                            FederationV1SchemaAnalyzer_InvalidDirectiveArguments,
                            schema.Name,
                            directive.Name,
                            member,
                            signature.ExpectedArguments)
                        .SetCode(LogEntryCodes.FederationV1DirectiveNotSupported)
                        .SetSeverity(LogSeverity.Error)
                        .SetSchema(schema)
                        .SetTypeSystemMember(provider)
                        .Build());
            }
        }
    }

    private static DirectiveLocation GetTypeLocation(ITypeDefinition type)
        => type switch
        {
            IObjectTypeDefinition => DirectiveLocation.Object,
            IInterfaceTypeDefinition => DirectiveLocation.Interface,
            IUnionTypeDefinition => DirectiveLocation.Union,
            IScalarTypeDefinition => DirectiveLocation.Scalar,
            IEnumTypeDefinition => DirectiveLocation.Enum,
            IInputObjectTypeDefinition => DirectiveLocation.InputObject,
            _ => 0
        };

    private sealed record DirectiveSignature(
        DirectiveLocation Locations,
        string? RequiredArgument,
        string ExpectedArguments);
}
