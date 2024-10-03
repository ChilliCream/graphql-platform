using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using IHasDirectives = HotChocolate.Types.IHasDirectives;

namespace HotChocolate.Caching;

internal sealed class CacheControlValidationTypeInterceptor : TypeInterceptor
{
    private ITypeCompletionContext _queryContext = default!;

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryContext = completionContext;
        }
    }

    public override void OnValidateType(
        ITypeSystemObjectContext validationContext,
        DefinitionBase definition)
    {
        if (validationContext.IsIntrospectionType)
        {
            return;
        }

        switch (validationContext.Type)
        {
            case ObjectType objectType:
            {
                var isQueryType = ReferenceEquals(validationContext, _queryContext);

                ValidateCacheControlOnType(validationContext, objectType);

                var span = objectType.Fields.AsSpan();

                for (var i = 0; i < span.Length; i++)
                {
                    var field = span[i];
                    ValidateCacheControlOnField(validationContext, field, objectType, isQueryType);
                }
                break;
            }

            case InterfaceType interfaceType:
            {
                ValidateCacheControlOnType(validationContext, interfaceType);

                var span = interfaceType.Fields.AsSpan();

                for (var i = 0; i < span.Length; i++)
                {
                    var field = span[i];
                    ValidateCacheControlOnField(validationContext, field, interfaceType, false);
                }
                break;
            }

            case UnionType unionType:
                ValidateCacheControlOnType(validationContext, unionType);
                break;
        }
    }

    private static void ValidateCacheControlOnType(
        ITypeSystemObjectContext validationContext,
        IHasDirectives type)
    {
        var directive = type.Directives
            .FirstOrDefault(CacheControlDirectiveType.Names.DirectiveName)?
            .AsValue<CacheControlDirective>();

        if (directive is null)
        {
            return;
        }

        if (directive.InheritMaxAge == true
            && type is ITypeSystemObject typeSystemObject)
        {
            var error = ErrorHelper.CacheControlInheritMaxAgeOnType(typeSystemObject);

            validationContext.ReportError(error);
        }
    }

    private static void ValidateCacheControlOnField(
        ITypeSystemObjectContext validationContext,
        IField field, ITypeSystemObject obj,
        bool isQueryTypeField)
    {
        var directive = field.Directives
            .FirstOrDefault(CacheControlDirectiveType.Names.DirectiveName)?
            .AsValue<CacheControlDirective>();

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
