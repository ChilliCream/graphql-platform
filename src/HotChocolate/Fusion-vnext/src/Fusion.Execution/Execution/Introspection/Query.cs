using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Introspection;

internal class Query : ITypeResolverInterceptor
{
    public void OnApplyResolver(string fieldName, IFeatureCollection features)
    {
        switch (fieldName)
        {
            case "__schema":
                features.Set(new ResolveFieldValue(Schema));
                break;

            case "__type":
                features.Set(new ResolveFieldValue(Type));
                break;
        }
    }

    public static void Schema(FieldContext context)
    {
        var result = context.RentInitializedObjectResult();
        context.FieldResult.SetNextValue(result);
        context.AddRuntimeResult(context.Schema);
    }

    public static void Type(FieldContext context)
    {
        var name = context.ArgumentValue<StringValueNode>("name");
        if (context.Schema.Types.TryGetType(name.Value, out var type))
        {
            var result = context.RentInitializedObjectResult();
            context.FieldResult.SetNextValue(result);
            context.AddRuntimeResult(type);
        }
    }
}
