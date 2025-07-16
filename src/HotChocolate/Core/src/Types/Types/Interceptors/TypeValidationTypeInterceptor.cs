using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Interceptors;

internal sealed class TypeValidationTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
        if (discoveryContext.IsIntrospectionType)
        {
            return;
        }

        switch (configuration)
        {
            case ObjectTypeConfiguration od:
                ValidateObjectType(discoveryContext, od);
                return;

            case InputObjectTypeConfiguration ind:
                ValidateInputObjectType(discoveryContext, ind);
                return;

            case InterfaceTypeConfiguration id:
                ValidateInterfaceType(discoveryContext, id);
                return;

            case UnionTypeConfiguration ud:
                ValidateUnionType(discoveryContext, ud);
                return;

            case DirectiveTypeConfiguration ud:
                ValidateDirectiveType(discoveryContext, ud);
                return;
        }
    }

    private void ValidateInputObjectType(
        ITypeDiscoveryContext context,
        InputObjectTypeConfiguration? definition)
    {
        if (definition is { RuntimeType: { } runtimeType }
            && IsTypeSystemType(definition.RuntimeType))
        {
            ReportRuntimeTypeError(context, runtimeType);
            definition.RuntimeType = typeof(object);
            definition.Fields.Clear();
            definition.Dependencies.Clear();
        }
    }

    private void ValidateDirectiveType(
        ITypeDiscoveryContext context,
        DirectiveTypeConfiguration? definition)
    {
        if (definition is { RuntimeType: { } runtimeType }
            && IsTypeSystemType(definition.RuntimeType))
        {
            ReportRuntimeTypeError(context, runtimeType);
            definition.RuntimeType = typeof(object);
            definition.Dependencies.Clear();
        }
    }

    private void ValidateUnionType(
        ITypeDiscoveryContext context,
        UnionTypeConfiguration? definition)
    {
        if (definition is { RuntimeType: { } runtimeType }
            && IsTypeSystemType(definition.RuntimeType))
        {
            ReportRuntimeTypeError(context, runtimeType);
            definition.RuntimeType = typeof(object);
            definition.Dependencies.Clear();
        }
    }

    private void ValidateObjectType(
        ITypeDiscoveryContext context,
        ObjectTypeConfiguration definition)
    {
        if (definition is { RuntimeType: { } runtimeType }
            && IsTypeSystemType(definition.RuntimeType))
        {
            ReportRuntimeTypeError(context, runtimeType);
            definition.RuntimeType = typeof(object);
            definition.Fields.Clear();
            definition.Interfaces.Clear();
            definition.Dependencies.Clear();
        }
    }

    private void ValidateInterfaceType(
        ITypeDiscoveryContext context,
        InterfaceTypeConfiguration? definition)
    {
        if (definition is { RuntimeType: { } runtimeType }
            && IsTypeSystemType(definition.RuntimeType))
        {
            ReportRuntimeTypeError(context, runtimeType);
            definition.RuntimeType = typeof(object);
            definition.Fields.Clear();
            definition.Interfaces.Clear();
            definition.Dependencies.Clear();
        }
    }

    private bool IsTypeSystemType(Type type) =>
        typeof(ITypeSystemMember).IsAssignableFrom(type);

    private void ReportRuntimeTypeError(
        ITypeDiscoveryContext discoveryContext,
        Type runtimeType)
    {
        var schemaError = ErrorHelper
            .NoSchemaTypesAllowedAsRuntimeType(discoveryContext.Type, runtimeType);
        discoveryContext.ReportError(schemaError);
    }
}
