using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.SchemaVisitors;

internal sealed class DiscoverLookupsSchemaVisitor(SchemaDefinition schema)
    : SchemaVisitor<DiscoverLookupsContext>
{
    private readonly MultiValueDictionary<string, ObjectTypeDefinition>
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
                        lookupFieldInfo.LookupField.Type.NamedType().Name,
                        lookupFieldInfo);
                }
            }
        }

        return lookupFieldGroupByTypeName;
    }

    // Overridden to avoid unnecessarily visiting directives.
    public override void VisitObjectType(ObjectTypeDefinition type, DiscoverLookupsContext context)
    {
        VisitOutputFields(type.Fields, context);
    }

    public override void VisitOutputField(
        OutputFieldDefinition field,
        DiscoverLookupsContext context)
    {
        if (field.Type.IsListType())
        {
            return;
        }

        List<ObjectTypeDefinition> objectTypesToVisit = [];

        // Lookup field.
        if (field.HasLookupDirective())
        {
            context.LookupFieldGroup.Add(
                new LookupFieldInfo(
                    field,
                    context.Path.Count == 0 ? null : string.Join(".", context.Path),
                    schema));

            switch (field.Type.NamedType())
            {
                case InterfaceTypeDefinition i:
                    objectTypesToVisit.AddRange(GetImplementingTypes(i));
                    break;

                case ObjectTypeDefinition o:
                    objectTypesToVisit.Add(o);
                    break;

                case UnionTypeDefinition u:
                    objectTypesToVisit.AddRange(u.Types);
                    break;
            }
        }
        // Lookup object.
        else if (field.Arguments.Count == 0 && field.Type.NamedType() is ObjectTypeDefinition o)
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
            if (type is ObjectTypeDefinition objectType)
            {
                foreach (var implementedInterface in objectType.Implements)
                {
                    _implementingTypesByInterfaceName.Add(implementedInterface.Name, objectType);
                }
            }
        }
    }

    private List<ObjectTypeDefinition> GetImplementingTypes(InterfaceTypeDefinition interfaceType)
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

    public HashSet<ObjectTypeDefinition> VisitedObjectTypes { get; } = [];

    public List<LookupFieldInfo> LookupFieldGroup { get; } = [];
}
