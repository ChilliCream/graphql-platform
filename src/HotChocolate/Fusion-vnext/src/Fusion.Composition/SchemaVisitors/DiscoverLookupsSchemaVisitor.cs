using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.SchemaVisitors;

internal sealed class DiscoverLookupsSchemaVisitor(MutableSchemaDefinition schema)
    : MutableSchemaDefinitionVisitor<DiscoverLookupsContext>
{
    private readonly MultiValueDictionary<string, MutableObjectTypeDefinition>
        _implementingTypesByInterfaceName = [];

    public MultiValueDictionary<string, LookupFieldInfo> Discover()
    {
        MultiValueDictionary<string, LookupFieldInfo> lookupFieldGroupByTypeName = [];

        if (schema.QueryType is not null)
        {
            LoadImplementingTypesByInterfaceName();

            foreach (var field in schema.QueryType.Fields)
            {
                var context = new DiscoverLookupsContext();

                VisitOutputField(field, context);

                foreach (var lookupFieldInfo in context.LookupFieldGroup)
                {
                    lookupFieldGroupByTypeName.Add(
                        lookupFieldInfo.LookupField.Type.AsTypeDefinition().Name,
                        lookupFieldInfo);
                }
            }
        }

        return lookupFieldGroupByTypeName;
    }

    // Overridden to avoid unnecessarily visiting directives.
    public override void VisitObjectType(MutableObjectTypeDefinition type, DiscoverLookupsContext context)
    {
        VisitOutputFields(type.Fields, context);
    }

    public override void VisitOutputField(
        MutableOutputFieldDefinition field,
        DiscoverLookupsContext context)
    {
        if (field.Type.IsListType())
        {
            return;
        }

        List<MutableObjectTypeDefinition> objectTypesToVisit = [];

        // Lookup field.
        if (field.HasLookupDirective())
        {
            context.LookupFieldGroup.Add(
                new LookupFieldInfo(
                    field,
                    context.Path.Count == 0 ? null : string.Join(".", context.Path),
                    schema));

            switch (field.Type.AsTypeDefinition())
            {
                case MutableInterfaceTypeDefinition i:
                    objectTypesToVisit.AddRange(GetImplementingTypes(i));
                    break;

                case MutableObjectTypeDefinition o:
                    objectTypesToVisit.Add(o);
                    break;

                case MutableUnionTypeDefinition u:
                    objectTypesToVisit.AddRange(u.Types);
                    break;
            }
        }
        // Lookup object.
        else if (field.Arguments.Count == 0 && field.Type.AsTypeDefinition() is MutableObjectTypeDefinition o)
        {
            objectTypesToVisit.Add(o);
        }

        if (objectTypesToVisit.Count > 0)
        {
            context.Path.Add(field.Name);

            foreach (var objectType in objectTypesToVisit)
            {
                if (context.VisitedObjectTypes.Add(objectType))
                {
                    VisitObjectType(objectType, context);

                    context.VisitedObjectTypes.Remove(objectType);
                }
            }

            context.Path.RemoveAt(context.Path.Count - 1);
        }
    }

    private void LoadImplementingTypesByInterfaceName()
    {
        foreach (var type in schema.Types)
        {
            if (type is MutableObjectTypeDefinition objectType)
            {
                foreach (var implementedInterface in objectType.Implements)
                {
                    _implementingTypesByInterfaceName.Add(implementedInterface.Name, objectType);
                }
            }
        }
    }

    private List<MutableObjectTypeDefinition> GetImplementingTypes(MutableInterfaceTypeDefinition interfaceType)
    {
        if (_implementingTypesByInterfaceName.TryGetValue(
            interfaceType.Name,
            out var implementingTypes))
        {
            return implementingTypes;
        }

        return [];
    }
}

internal class DiscoverLookupsContext
{
    public List<string> Path { get; } = [];

    public HashSet<MutableObjectTypeDefinition> VisitedObjectTypes { get; } = [];

    public List<LookupFieldInfo> LookupFieldGroup { get; } = [];
}
