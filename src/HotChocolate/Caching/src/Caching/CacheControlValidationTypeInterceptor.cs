using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Caching;

internal sealed class CacheControlValidationTypeInterceptor : TypeInterceptor
{
    private ITypeCompletionContext _queryContext = null!;

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeConfiguration configuration,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryContext = completionContext;
        }
    }

    public override void OnValidateType(
        ITypeSystemObjectContext context,
        TypeSystemConfiguration configuration)
    {
        if (context.IsIntrospectionType)
        {
            return;
        }

        switch (context.Type)
        {
            case ObjectType objectType:
                {
                    var isQueryType = ReferenceEquals(context, _queryContext);

                    ValidateCacheControlOnType(context, objectType);

                    var span = objectType.Fields.AsSpan();

                    for (var i = 0; i < span.Length; i++)
                    {
                        var field = span[i];
                        ValidateCacheControlOnField(context, field, objectType, isQueryType);
                    }
                    break;
                }

            case InterfaceType interfaceType:
                {
                    ValidateCacheControlOnType(context, interfaceType);

                    var span = interfaceType.Fields.AsSpan();

                    for (var i = 0; i < span.Length; i++)
                    {
                        var field = span[i];
                        ValidateCacheControlOnField(context, field, interfaceType, false);
                    }
                    break;
                }

            case UnionType unionType:
                ValidateCacheControlOnType(context, unionType);
                break;
        }
    }

    private static void ValidateCacheControlOnType(
        ITypeSystemObjectContext validationContext,
        IDirectivesProvider type)
    {
        var directive = (type.Directives
            .FirstOrDefault(CacheControlDirectiveType.Names.DirectiveName) as Directive)
            ?.ToValue<CacheControlDirective>();

        if (directive is null)
        {
            return;
        }

        if (directive.InheritMaxAge == true
            && type is TypeSystemObject typeSystemObject)
        {
            var error = ErrorHelper.CacheControlInheritMaxAgeOnType(typeSystemObject);

            validationContext.ReportError(error);
        }
    }

    private static void ValidateCacheControlOnField(
        ITypeSystemObjectContext validationContext,
        IFieldDefinition field,
        TypeSystemObject obj,
        bool isQueryTypeField)
    {
        var directive = (field.Directives
            .FirstOrDefault(CacheControlDirectiveType.Names.DirectiveName) as Directive)
            ?.ToValue<CacheControlDirective>();

        if (directive is null)
        {
            return;
        }

        if (field is InterfaceField)
        {
            var error = ErrorHelper.CacheControlOnInterfaceField(obj, field);
            validationContext.ReportError(error);
            return;
        }

        var inheritMaxAge = directive.InheritMaxAge == true;

        if (isQueryTypeField && inheritMaxAge)
        {
            var error = ErrorHelper.CacheControlInheritMaxAgeOnQueryTypeField(obj, field);
            validationContext.ReportError(error);
        }

        if (directive.MaxAge.HasValue)
        {
            if (directive.MaxAge.Value < 0)
            {
                var error = ErrorHelper.CacheControlNegativeMaxAge(obj, field);
                validationContext.ReportError(error);
            }

            if (inheritMaxAge)
            {
                var error = ErrorHelper.CacheControlBothMaxAgeAndInheritMaxAge(obj, field);
                validationContext.ReportError(error);
            }
        }

        if (directive.SharedMaxAge.HasValue)
        {
            if (directive.SharedMaxAge.Value < 0)
            {
                var error = ErrorHelper.CacheControlNegativeSharedMaxAge(obj, field);
                validationContext.ReportError(error);
            }

            if (inheritMaxAge)
            {
                var error = ErrorHelper.CacheControlBothSharedMaxAgeAndInheritMaxAge(obj, field);
                validationContext.ReportError(error);
            }
        }
    }
}
