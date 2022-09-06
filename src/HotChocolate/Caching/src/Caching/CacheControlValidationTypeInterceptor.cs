using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Caching;

internal sealed class CacheControlValidationTypeInterceptor : TypeInterceptor
{
    public override void OnValidateType(ITypeSystemObjectContext validationContext,
        DefinitionBase? definition, IDictionary<string, object?> contextData)
    {
        if (validationContext.IsIntrospectionType)
        {
            return;
        }

        if (validationContext.Type is ObjectType objectType)
        {
            var isQueryType = validationContext is ITypeCompletionContext completionContext &&
                completionContext.IsQueryType == true;

            ValidateCacheControlOnType(validationContext, objectType);

            foreach (var field in objectType.Fields)
            {
                ValidateCacheControlOnField(validationContext, field, objectType,
                    isQueryType);
            }
        }
        else if (validationContext.Type is InterfaceType interfaceType)
        {
            ValidateCacheControlOnType(validationContext, interfaceType);

            foreach (var field in interfaceType.Fields)
            {
                ValidateCacheControlOnField(validationContext, field, interfaceType,
                    false);
            }
        }
        else if (validationContext.Type is UnionType unionType)
        {
            ValidateCacheControlOnType(validationContext, unionType);
        }
    }

    private static void ValidateCacheControlOnType(
        ITypeSystemObjectContext validationContext,
        IHasDirectives type)
    {
        var directive = type.Directives
            .FirstOrDefault(d => d.Name == CacheControlDirectiveType.DirectiveName)
            ?.ToObject<CacheControlDirective>();

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
                    .FirstOrDefault(d => d.Name == CacheControlDirectiveType.DirectiveName)
                    ?.ToObject<CacheControlDirective>();

        if (directive is null)
        {
            return;
        }

        if (field is InterfaceField interfaceField)
        {
            var error = ErrorHelper
                    .CacheControlOnInterfaceField(obj, field);

            validationContext.ReportError(error);

            return;
        }

        var inheritMaxAge = directive.InheritMaxAge == true;

        if (isQueryTypeField && inheritMaxAge)
        {
            var error =
                ErrorHelper.CacheControlInheritMaxAgeOnQueryTypeField(obj, field);

            validationContext.ReportError(error);
        }

        if (directive.MaxAge.HasValue)
        {
            if (directive.MaxAge.Value < 0)
            {
                var error = ErrorHelper
                    .CacheControlNegativeMaxAge(obj, field);

                validationContext.ReportError(error);
            }

            if (inheritMaxAge)
            {
                var error = ErrorHelper
                    .CacheControlBothMaxAgeAndInheritMaxAge(obj, field);

                validationContext.ReportError(error);
            }
        }
    }
}
