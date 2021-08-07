using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

                foreach (var inputField in def.Fields)
                {
                    if (inputField is FilterFieldDefinition field &&
                        field.Member is not null)
                    {
                        ColumnAttribute? columnAttribute = field.Member
                            .GetCustomAttributes(typeof(ColumnAttribute), true)
                            .OfType<ColumnAttribute>()
                            .FirstOrDefault();

                        var fieldName = field.Member.Name;
                        if (columnAttribute is not null)
                        {
                            fieldName = columnAttribute.Name ?? field.Member.Name;
                            field.ContextData[SqlKataContextData.ColumnName] = fieldName;
                        }

                        ForeignKeyAttribute? foreignKey = field.Member
                            .GetCustomAttributes(typeof(ForeignKeyAttribute), true)
                            .OfType<ForeignKeyAttribute>()
                            .FirstOrDefault();

                        if (foreignKey is not null)
                        {
                            field.ContextData[SqlKataContextData.ForeignKey] = foreignKey.Name;
                        }

                        KeyAttribute? keyAttribute = field.Member
                            .GetCustomAttributes(typeof(KeyAttribute), true)
                            .OfType<KeyAttribute>()
                            .FirstOrDefault();

                        if (keyAttribute is not null)
                        {
                            def.ContextData[SqlKataContextData.KeyName] = fieldName;
                        }
                    }
                }
            }
        }
    }
}
