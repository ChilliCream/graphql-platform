using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.SqlKata.Filters
{
    public class SqlKataDataAnnotationsTypeInterceptor
        : TypeInterceptor
    {
        public override bool CanHandle(ITypeSystemObjectContext context) =>
            !context.IsIntrospectionType;

        public override void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is FilterInputTypeDefinition def)
            {
                if (def.EntityType is { } entityType)
                {
                    TableAttribute? tableAttribute = entityType
                        .GetCustomAttributes(typeof(TableAttribute), true)
                        .OfType<TableAttribute>()
                        .FirstOrDefault();

                    if (tableAttribute is not null)
                    {
                        def.ContextData[SqlKataContextData.TableName] = tableAttribute.Name;
                    }
                }

                foreach (var field in def.Fields)
                {
                    if (field.Property is not null)
                    {
                        ColumnAttribute? columnAttribute = field.Property
                            .GetCustomAttributes(typeof(ColumnAttribute), true)
                            .OfType<ColumnAttribute>()
                            .FirstOrDefault();

                        if (columnAttribute is not null)
                        {
                            field.ContextData[SqlKataContextData.ColumnName] = columnAttribute.Name;
                        }
                    }
                }
            }
        }
    }
}
