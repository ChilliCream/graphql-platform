using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class Issue9908Tests
{
    [Fact]
    public async Task MethodInfo_And_ParameterInfo_Set()
    {
        await new ServiceCollection()
            .AddGraphQLServer(disableDefaultSecurity: true)
            .AddIntegrationTestTypes()
            .TryAddTypeInterceptor<CustomDirectiveInterceptor>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    public class CustomDirectiveInterceptor : TypeInterceptor
    {
        /// <summary>
        /// Apply directives from type attributes.
        /// </summary>
        public override void OnBeforeRegisterDependencies(ITypeDiscoveryContext discoveryContext, TypeSystemConfiguration definition)
        {
            if (definition is ObjectTypeConfiguration objectTypeDefinition)
            {
                // Location: OBJECT
                if (definition.Name.StartsWith("Issue9908") && objectTypeDefinition.RuntimeType is null)
                {
                    throw new Exception("Object runtime type is not set.");
                }
                definition.Description = "Object runtime type is set.";

                foreach (var field in objectTypeDefinition.Fields)
                {
                    // Location: FIELD_DEFINITION
                    if (field.Name.StartsWith("issue9908"))
                    {
                        if (field.Member is null)
                        {
                            throw new Exception("Field member is not set.");
                        }
                        field.Description = "Field member is set.";
                    }

                    if (field.Arguments?.Count > 0)
                    {
                        foreach (var argument in field.Arguments)
                        {
                            // Location: ARGUMENT_DEFINITION
                            if (argument.Name.StartsWith("issue9908"))
                            {
                                if (argument.Parameter is null)
                                {
                                    throw new Exception("Argument parameter is not set.");
                                }
                                argument.Description = "Argument parameter is set.";
                            }
                        }
                    }
                }
            }

            if (definition is InputObjectTypeConfiguration inputObjectTypeDefinition)
            {
                // Location: INPUT_OBJECT
                if (definition.Name.StartsWith("Issue9908"))
                {
                    if (inputObjectTypeDefinition.RuntimeType is null)
                    {
                        throw new Exception("Object runtime type is not set.");
                    }
                    inputObjectTypeDefinition.Description = "Input object runtime type is set.";
                }

                foreach (var field in inputObjectTypeDefinition.Fields)
                {
                    // Location: INPUT_FIELD_DEFINITION
                    if (field.Name.StartsWith("issue9908"))
                    {
                        if (field.Property is null)
                        {
                            throw new Exception("Field property is not set.");
                        }
                        field.Description = "Field property is set.";
                    }
                }
            }
        }
    }
}

[QueryType]
public static partial class Queries
{
    public static Issue9908ResponseObject Issue9908Method(string name, Issue9908InputObject input)
        => new() { Issue9908Property = $"Hello, {name}!" };
}

public class Issue9908InputObject
{
    public string Issue9908Property { get; set; } = default!;
}

public class Issue9908ResponseObject
{
    public string Issue9908Property { get; set; } = default!;
}
