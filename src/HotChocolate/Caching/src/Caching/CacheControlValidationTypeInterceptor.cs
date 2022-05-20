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
        if (validationContext.Type is not ObjectType obj)
        {
            return;
        }

        foreach (ObjectField field in obj.Fields)
        {
            CacheControlDirective? directive = field.Directives
                .FirstOrDefault(d => d.Name == CacheControlDirectiveType.DirectiveName)
                ?.ToObject<CacheControlDirective>();

            if (directive is null)
            {
                continue;
            }

            if (directive.MaxAge.HasValue)
            {
                if (directive.MaxAge.Value < 0)
                {
                    // todo: error helper and more information about location
                    ISchemaError error = SchemaErrorBuilder.New()
                                .SetMessage("Value of `maxAge` on @cacheControl directive can not be negative.")
                                .Build();

                    validationContext.ReportError(error);
                }

                if (directive.InheritMaxAge == true)
                {
                    // todo: error helper and more information about location
                    ISchemaError error = SchemaErrorBuilder.New()
                                .SetMessage("@cacheControl directive can not specify `inheritMaxAge: true` and a value for `maxAge`.")
                                .Build();

                    validationContext.ReportError(error);
                }
            }
        }
    }
}
