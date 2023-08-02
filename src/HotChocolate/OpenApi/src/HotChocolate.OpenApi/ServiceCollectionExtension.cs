using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Skimmed;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using InputObjectType = HotChocolate.Skimmed.InputObjectType;
using ObjectType = HotChocolate.Skimmed.ObjectType;

namespace HotChocolate.OpenApi;

public static class ServiceCollectionExtension
{
    public static IRequestExecutorBuilder AddOpenApi(this IRequestExecutorBuilder requestExecutorBuilder, string openApi)
    {
        var document = new OpenApiStringReader().Read(openApi, out _);
        requestExecutorBuilder.ParseAndAddTypes(document);
        return requestExecutorBuilder;
    }

    public static IRequestExecutorBuilder AddOpenApi(this IRequestExecutorBuilder requestExecutorBuilder, Stream openApiStream)
    {
        var document = new OpenApiStreamReader().Read(openApiStream, out _);
        requestExecutorBuilder.ParseAndAddTypes(document);
        return requestExecutorBuilder;
    }

    private static void ParseAndAddTypes(this IRequestExecutorBuilder requestExecutorBuilder,
        OpenApiDocument apiDocument)
    {
        requestExecutorBuilder.AddJsonSupport();
        requestExecutorBuilder.InitializeSchema(new OpenApiWrapper().Wrap(apiDocument));
    }

    private static void InitializeSchema(
        this IRequestExecutorBuilder requestExecutorBuilder,
        Skimmed.Schema schema)
    {
        if (schema.QueryType is { } queryType)
        {
            requestExecutorBuilder.AddQueryType(SetupType(queryType));
        }

        if (schema.MutationType is { } mutationType)
        {
            requestExecutorBuilder.AddMutationType(SetupType(mutationType));
        }

        foreach (var type in schema.Types.OfType<ObjectType>())
        {
            requestExecutorBuilder.AddObjectType(SetupType(type));
        }

        foreach (var type in schema.Types.OfType<InputObjectType>())
        {
            requestExecutorBuilder.AddInputObjectType(SetupInputType(type));
        }
    }

    private static Action<IObjectTypeDescriptor> SetupType(ComplexType skimmedType) =>
        desc =>
        {
            desc.Name(skimmedType.Name)
                .Description(skimmedType.Description);

            foreach (var field in skimmedType.Fields)
            {
                var fieldDescriptor = desc.Field(field.Name)
                    .Description(field.Description)
                    .Type(new NamedTypeNode(field.Type.NamedType().Name));

                foreach (var fieldArgument in field.Arguments)
                {
                    fieldDescriptor.Argument(fieldArgument.Name, descriptor => descriptor
                        .Type(new NamedTypeNode(fieldArgument.Type.NamedType().Name)));
                }

                if (field.ContextData.TryGetValue("resolver", out var res) &&
                    res is Func<IResolverContext, Task<string>> resolver)
                {
                    fieldDescriptor.Resolve(async ctx => await resolver.Invoke(ctx));
                }
                else
                {
                    fieldDescriptor.FromJson();
                }
            }
        };

    private static Action<IInputObjectTypeDescriptor> SetupInputType(InputObjectType skimmedType) =>
        desc =>
        {
            desc.Name(skimmedType.Name)
                .Description(skimmedType.Description);

            foreach (var field in skimmedType.Fields)
            {
                desc.Field(field.Name)
                    .Description(field.Description)
                    .Type(new NamedTypeNode(field.Type.NamedType().Name));
            }
        };
}
