using System.Text;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.Types;

internal sealed class QueryConventionTypeInterceptor : TypeInterceptor
{
    private readonly StringBuilder _sb = new();
    private readonly List<ObjectTypeDefinition> _typeDefs = new();
    private TypeInitializer _typeInitializer = default!;
    private TypeRegistry _typeRegistry = default!;
    private TypeLookup _typeLookup = default!;
    private IDescriptorContext _context = default!;
    private ObjectTypeDefinition _mutationDef = default!;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _context = context;
        _typeInitializer = typeInitializer;
        _typeRegistry = typeRegistry;
        _typeLookup = typeLookup;
    }

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        if (operationType is OperationType.Mutation)
        {
            _mutationDef = definition;
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext, 
        DefinitionBase definition)
    {
        if (completionContext.Type is ObjectType &&
            definition is ObjectTypeDefinition typeDef)
        {
            _typeDefs.Add(typeDef);
        }
        
        base.OnAfterCompleteName(completionContext, definition);
    }

    public override void OnBeforeCompleteTypes()
    {
        foreach (var typeDef in _typeDefs)
        {
            if (_mutationDef == typeDef)
            {
                continue;
            }

            foreach (var field in typeDef.Fields)
            {
                if (typeof(IFieldResult).IsAssignableFrom(field.ResultType))
                {
                    field.Type = CreateFieldResultType(field.Name, field.Type!);
                    typeDef.Dependencies.Add(new TypeDependency(field.Type, TypeDependencyFulfilled.Completed));
                }
            }
        }
    }

    private TypeReference CreateFieldResultType(string fieldName, TypeReference typeRef)
    {
        if (!_context.TryInferSchemaType(typeRef, out var schemaTypeRefs))
        {
            throw new Exception();
        }

        var resultType = new UnionType(
            d =>
            {
                d.Name(CreateResultTypeName(fieldName));
                
                var typeDef = d.Extend().Definition;
                
                foreach (var schemaTypeRef in schemaTypeRefs)
                {
                    typeDef.Types.Add(schemaTypeRef);
                }
            });

        var registeredType = _typeInitializer.InitializeType(resultType);
        var resultTypeRef = new SchemaTypeReference(new NonNullType(resultType));
        registeredType.References.Add(resultTypeRef);
        _typeRegistry.Register(registeredType);
        
        _typeInitializer.CompleteTypeName(registeredType);
        _typeInitializer.CompleteType(registeredType);

        return resultTypeRef;
    }

    private string CreateResultTypeName(string fieldName)
    {
        _sb.Clear();
        _sb.Append(char.ToUpper(fieldName[0]));

        if (fieldName.Length > 1)
        {
#if NET6_0_OR_GREATER
            _sb.Append(fieldName.AsSpan()[1..]);
#else
            _sb.Append(fieldName.Substring(1));
#endif
        }

        _sb.Append("Result");

        return _sb.ToString();
    }
}