using System.Text;
using static HotChocolate.Types.Descriptors.Definitions.TypeDependencyFulfilled;

namespace HotChocolate.Types;

internal sealed class QueryConventionTypeInterceptor : TypeInterceptor
{
    private readonly ErrorTypeHelper _errorTypeHelper = new();
    private readonly StringBuilder _sb = new();
    private readonly List<ObjectTypeDefinition> _typeDefs = new();
    private TypeInitializer _typeInitializer = default!;
    private TypeRegistry _typeRegistry = default!;
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
        _errorTypeHelper.InitializerErrorTypeInterface(_context);
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
                if (field.IsIntrospectionField)
                {
                    continue;
                }
                
                List<TypeReference>? errors = null;
                
                if (typeof(IFieldResult).IsAssignableFrom(field.ResultType) &&
                    _context.TryInferSchemaType(field.Type!, out var schemaTypeRefs))
                {
                    (errors ??= []).AddRange(schemaTypeRefs);
                }

                var errorDefinitions = _errorTypeHelper.GetErrorDefinitions(field);

                if (errorDefinitions.Count > 0)
                {
                    var errorInterfaceIsRegistered = false;
                    var errorInterfaceTypeRef = _errorTypeHelper.ErrorTypeInterfaceRef;

                    foreach (var errorDef in errorDefinitions)
                    {
                        var obj = TryRegisterType(errorDef.SchemaType);

                        if (obj is not null)
                        {
                            _typeRegistry.Register(obj);
                            var errorTypeRef = TypeReference.Create(obj.Type);
                            RegisterErrorType(errorTypeRef, errorDef.SchemaType);
                            RegisterErrorType(errorTypeRef, errorDef.RuntimeType);
                            ((ObjectType)obj.Type).Definition!.Interfaces.Add(errorInterfaceTypeRef);
                        }
                        
                        (errors ??= []).Add(_context.TypeInspector.GetOutputTypeRef(errorDef.SchemaType));

                        if (!errorInterfaceIsRegistered && _typeRegistry.TryGetTypeRef(errorInterfaceTypeRef, out _))
                        {
                            continue;
                        }

                        var err = TryRegisterType(errorInterfaceTypeRef.Type.Type);

                        if (err is not null)
                        {
                            err.References.Add(errorInterfaceTypeRef);
                            _typeRegistry.Register(err);
                            _typeRegistry.TryRegister(errorInterfaceTypeRef, TypeReference.Create(err.Type));
                        }
                        errorInterfaceIsRegistered = true;
                    }
                }

                if (errors?.Count > 0)
                {
                    field.Type = CreateFieldResultType(field.Name, errors);
                    typeDef.Dependencies.Add(new TypeDependency(field.Type, Completed));
                }
            }
        }
    }

    private TypeReference CreateFieldResultType(string fieldName, IReadOnlyList<TypeReference> errors)
    {
        var resultType = new UnionType(
            d =>
            {
                d.Name(CreateResultTypeName(fieldName));
                
                var typeDef = d.Extend().Definition;
                
                foreach (var schemaTypeRef in errors)
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
    
    private RegisteredType? TryRegisterType(Type type)
    {
        if (_typeRegistry.IsRegistered(_context.TypeInspector.GetOutputTypeRef(type)))
        {
            return null;
        }

        var registeredType = _typeInitializer.InitializeType(type);
        _typeInitializer.CompleteTypeName(registeredType);

        if (registeredType.Type is ObjectType errorObject &&
            errorObject.RuntimeType != typeof(object))
        {
            foreach (var possibleInterface in _typeRegistry.Types)
            {
                if (possibleInterface.Type is InterfaceType interfaceType &&
                    interfaceType.RuntimeType != typeof(object) &&
                    interfaceType.RuntimeType.IsAssignableFrom(errorObject.RuntimeType))
                {
                    var typeRef = possibleInterface.TypeReference;
                    errorObject.Definition!.Interfaces.Add(typeRef);
                    registeredType.Dependencies.Add(new(typeRef, Completed));
                }
                else if (possibleInterface.Type is UnionType unionType &&
                    unionType.RuntimeType != typeof(object) &&
                    unionType.RuntimeType.IsAssignableFrom(errorObject.RuntimeType))
                {
                    var typeRef = registeredType.TypeReference;
                    unionType.Definition!.Types.Add(typeRef);
                    possibleInterface.Dependencies.Add(new(typeRef, Completed));
                }
            }
        }
        else if (registeredType.Type is ObjectType errorInterface &&
            errorInterface.RuntimeType != typeof(object))
        {
            foreach (var possibleInterface in _typeRegistry.Types)
            {
                if (possibleInterface.Type is InterfaceType interfaceType &&
                    interfaceType.RuntimeType != typeof(object) &&
                    interfaceType.RuntimeType.IsAssignableFrom(errorInterface.RuntimeType))
                {
                    var typeRef = possibleInterface.TypeReference;
                    errorInterface.Definition!.Interfaces.Add(typeRef);
                    registeredType.Dependencies.Add(new(typeRef, Completed));
                }
            }
        }

        return registeredType;
    }

    private void RegisterErrorType(TypeReference errorTypeRef, Type lookupType)
        => _typeRegistry.TryRegister(_context.TypeInspector.GetOutputTypeRef(lookupType), errorTypeRef);

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