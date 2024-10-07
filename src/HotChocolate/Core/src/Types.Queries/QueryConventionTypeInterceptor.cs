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

    public override void OnAfterResolveRootType(
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
        if (completionContext.Type is ObjectType && definition is ObjectTypeDefinition typeDef)
        {
            _typeDefs.Add(typeDef);
        }

        base.OnAfterCompleteName(completionContext, definition);
    }

    public override void OnBeforeCompleteTypes()
    {
        var errorInterfaceTypeRef = _errorTypeHelper.ErrorTypeInterfaceRef;
        var errorInterfaceIsRegistered = false;
        List<TypeReference>? typeSet = null;

        foreach (var typeDef in _typeDefs.ToArray())
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

                typeSet?.Clear();

                if (field.ResultType != null
                    && typeof(IFieldResult).IsAssignableFrom(field.ResultType)
                    && _context.TryInferSchemaType(
                        _context.TypeInspector.GetOutputTypeRef(field.ResultType),
                        out var schemaTypeRefs))
                {
                    foreach (var errorTypeRef in schemaTypeRefs.Skip(1))
                    {
                        (typeSet ??= []).Add(errorTypeRef);

                        if (_typeRegistry.TryGetType(errorTypeRef, out var errorType))
                        {
                            ((ObjectType)errorType.Type).Definition!.Interfaces.Add(errorInterfaceTypeRef);
                        }
                    }
                }

                // collect error definitions from query field.
                var errorDefinitions = _errorTypeHelper.GetErrorDefinitions(field);

                // collect error factories for middleware
                var errorFactories = errorDefinitions.Count != 0
                    ? errorDefinitions.Select(t => t.Factory).ToArray()
                    : [];

                if (errorDefinitions.Count > 0)
                {
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

                        var errorSchemaTypeRef = _context.TypeInspector.GetOutputTypeRef(errorDef.SchemaType);

                        if (obj is null && _typeRegistry.TryGetType(errorSchemaTypeRef, out obj))
                        {
                            ((ObjectType)obj.Type).Definition!.Interfaces.Add(errorInterfaceTypeRef);
                        }

                        (typeSet ??= []).Add(errorSchemaTypeRef);
                    }
                }

                if (!errorInterfaceIsRegistered && !_typeRegistry.TryGetTypeRef(errorInterfaceTypeRef, out _))
                {
                    var err = TryRegisterType(errorInterfaceTypeRef.Type.Type);

                    if (err is not null)
                    {
                        err.References.Add(errorInterfaceTypeRef);
                        _typeRegistry.Register(err);
                        _typeRegistry.TryRegister(errorInterfaceTypeRef, TypeReference.Create(err.Type));
                    }

                    errorInterfaceIsRegistered = true;
                }

                if (typeSet?.Count > 0)
                {
                    typeSet.Insert(0, GetFieldType(field.Type!));
                    field.Type = CreateFieldResultType(field.Name, typeSet);
                    typeDef.Dependencies.Add(new TypeDependency(field.Type, Completed));

                    // create middleware
                    var errorMiddleware =
                        new FieldMiddlewareDefinition(
                            FieldClassMiddlewareFactory.Create<QueryResultMiddleware>(
                                (typeof(IReadOnlyList<CreateError>), errorFactories)),
                            key: "Query Results",
                            isRepeatable: false);

                    // last but not least we insert the result middleware to the query field.
                    field.MiddlewareDefinitions.Insert(0, errorMiddleware);
                }
            }
        }
    }

    private static TypeReference GetFieldType(TypeReference typeRef)
    {
        if (typeRef is ExtendedTypeReference { Type.IsGeneric: true, } extendedTypeRef
            && typeof(IFieldResult).IsAssignableFrom(extendedTypeRef.Type.Type))
        {
            return TypeReference.Create(extendedTypeRef.Type.TypeArguments[0], TypeContext.Output);
        }

        return typeRef;
    }

    private TypeReference CreateFieldResultType(string fieldName, IReadOnlyList<TypeReference> typeSet)
    {
        var resultType = new UnionType(
            d =>
            {
                d.Name(CreateResultTypeName(fieldName));

                var typeDef = d.Extend().Definition;

                foreach (var schemaTypeRef in typeSet)
                {
                    typeDef.Types.Add(schemaTypeRef);
                }
            });

        var registeredType = _typeInitializer.InitializeType(resultType);
        var resultTypeRef = new SchemaTypeReference(new NonNullType(resultType));
        registeredType.References.Add(resultTypeRef);
        _typeRegistry.Register(registeredType);

        _typeInitializer.CompleteTypeName(registeredType);
        _typeInitializer.CompileResolvers(registeredType);
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
        _typeInitializer.CompileResolvers(registeredType);

        if (registeredType.Type is ObjectType errorObject && errorObject.RuntimeType != typeof(object))
        {
            foreach (var possibleInterface in _typeRegistry.Types)
            {
                if (possibleInterface.Type is InterfaceType interfaceType
                    && interfaceType.RuntimeType != typeof(object)
                    && interfaceType.RuntimeType.IsAssignableFrom(errorObject.RuntimeType))
                {
                    var typeRef = possibleInterface.TypeReference;
                    errorObject.Definition!.Interfaces.Add(typeRef);
                    registeredType.Dependencies.Add(new TypeDependency(typeRef, Completed));
                }
                else if (possibleInterface.Type is UnionType unionType
                    && unionType.RuntimeType != typeof(object)
                    && unionType.RuntimeType.IsAssignableFrom(errorObject.RuntimeType))
                {
                    var typeRef = registeredType.TypeReference;
                    unionType.Definition!.Types.Add(typeRef);
                    possibleInterface.Dependencies.Add(new TypeDependency(typeRef, Completed));
                }
            }
        }
        else if (registeredType.Type is ObjectType errorInterface && errorInterface.RuntimeType != typeof(object))
        {
            foreach (var possibleInterface in _typeRegistry.Types)
            {
                if (possibleInterface.Type is InterfaceType interfaceType
                    && interfaceType.RuntimeType != typeof(object)
                    && interfaceType.RuntimeType.IsAssignableFrom(errorInterface.RuntimeType))
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
            _sb.Append(fieldName.AsSpan()[1..]);
        }

        _sb.Append("Result");

        return _sb.ToString();
    }
}
