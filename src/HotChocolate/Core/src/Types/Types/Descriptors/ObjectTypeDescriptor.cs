using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public class ObjectTypeDescriptor
    : DescriptorBase<ObjectTypeDefinition>
    , IObjectTypeDescriptor
{
    protected ObjectTypeDescriptor(IDescriptorContext context, Type clrType)
        : base(context)
    {
        if (clrType is null)
        {
            throw new ArgumentNullException(nameof(clrType));
        }

        Definition.RuntimeType = clrType;
        Definition.Name = context.Naming.GetTypeName(clrType, TypeKind.Object);
        Definition.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Object);
    }

    protected ObjectTypeDescriptor(IDescriptorContext context)
        : base(context)
    {
        Definition.RuntimeType = typeof(object);
    }

    protected ObjectTypeDescriptor(
        IDescriptorContext context,
        ObjectTypeDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));

        foreach (var field in definition.Fields)
        {
            Fields.Add(ObjectFieldDescriptor.From(Context, field));
        }
    }

    protected internal override ObjectTypeDefinition Definition { get; protected set; } = new();

    protected ICollection<ObjectFieldDescriptor> Fields { get; } =
        new List<ObjectFieldDescriptor>();

    protected override void OnCreateDefinition(
        ObjectTypeDefinition definition)
    {
        if (!Definition.AttributesAreApplied && Definition.FieldBindingType is not null)
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Definition.FieldBindingType);
            Definition.AttributesAreApplied = true;
        }

        foreach (var field in Fields)
        {
            if (!field.Definition.Ignore)
            {
                continue;
            }

            // if this definition is used for a type extension we need a
            // binding to a field which shall be ignored. In case this is a
            // definition for the type it will be ignored by the type initialization.
            Definition.FieldIgnores.Add(new(field.Definition.Name, ObjectFieldBindingType.Field));
        }

        var fields = new Dictionary<string, ObjectFieldDefinition>();
        var handledMembers = new HashSet<MemberInfo>();

        FieldDescriptorUtilities.AddExplicitFields(
            Fields.Select(t => t.CreateDefinition()),
            f => f.Member,
            fields,
            handledMembers);

        OnCompleteFields(fields, handledMembers);

        Definition.Fields.Clear();
        Definition.Fields.AddRange(fields.Values);

        base.OnCreateDefinition(definition);
    }

    protected virtual void OnCompleteFields(
        IDictionary<string, ObjectFieldDefinition> fields,
        ISet<MemberInfo> handledMembers)
    {

    }

    public IObjectTypeDescriptor SyntaxNode(
        ObjectTypeDefinitionNode? objectTypeDefinition)
    {
        Definition.SyntaxNode = objectTypeDefinition;
        return this;
    }

    public IObjectTypeDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public IObjectTypeDescriptor Description(string? value)
    {
        Definition.Description = value;
        return this;
    }

    [Obsolete("Use Implements.")]
    public IObjectTypeDescriptor Interface<TInterface>()
        where TInterface : InterfaceType
        => Implements<TInterface>();

    [Obsolete("Use Implements.")]
    public IObjectTypeDescriptor Interface<TInterface>(
        TInterface type)
        where TInterface : InterfaceType
        => Implements(type);

    [Obsolete("Use Implements.")]
    public IObjectTypeDescriptor Interface(
        NamedTypeNode namedType)
        => Implements(namedType);

    public IObjectTypeDescriptor Implements<T>()
        where T : InterfaceType
    {
        if (typeof(T) == typeof(InterfaceType))
        {
            throw new ArgumentException(
                ObjectTypeDescriptor_InterfaceBaseClass);
        }

        Definition.Interfaces.Add(
            Context.TypeInspector.GetTypeRef(typeof(T)));
        return this;
    }

    public IObjectTypeDescriptor Implements<T>(T type)
        where T : InterfaceType
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        Definition.Interfaces.Add(new SchemaTypeReference(
            type));
        return this;
    }

    public IObjectTypeDescriptor Implements(NamedTypeNode type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        Definition.Interfaces.Add(TypeReference.Create(type, TypeContext.Output));
        return this;
    }

    public IObjectTypeDescriptor IsOfType(IsOfType? isOfType)
    {
        Definition.IsOfType = isOfType
            ?? throw new ArgumentNullException(nameof(isOfType));
        return this;
    }

    public IObjectFieldDescriptor Field(string name)
    {
        var fieldDescriptor = Fields.FirstOrDefault(t => t.Definition.Name.EqualsOrdinal(name));

        if (fieldDescriptor is not null)
        {
            return fieldDescriptor;
        }

        fieldDescriptor = ObjectFieldDescriptor.New(Context, name);
        Fields.Add(fieldDescriptor);
        return fieldDescriptor;
    }

    public IObjectFieldDescriptor Field<TResolver>(
        Expression<Func<TResolver, object?>> propertyOrMethod) =>
        Field<TResolver, object?>(propertyOrMethod);

    public IObjectFieldDescriptor Field(
        MemberInfo propertyOrMethod)
    {
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        if (propertyOrMethod is PropertyInfo || propertyOrMethod is MethodInfo)
        {
            var fieldDescriptor = Fields.FirstOrDefault(
                t => t.Definition.Member == propertyOrMethod);

            if (fieldDescriptor is not null)
            {
                return fieldDescriptor;
            }

            fieldDescriptor = ObjectFieldDescriptor.New(
                Context,
                propertyOrMethod,
                Definition.RuntimeType,
                propertyOrMethod.ReflectedType ?? Definition.RuntimeType);
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        throw new ArgumentException(
            ObjectTypeDescriptor_MustBePropertyOrMethod,
            nameof(propertyOrMethod));
    }

    public IObjectFieldDescriptor Field<TResolver, TPropertyType>(
        Expression<Func<TResolver, TPropertyType>> propertyOrMethod)
    {
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        var member = propertyOrMethod.TryExtractMember();

        if (member is PropertyInfo or MethodInfo)
        {
            var fieldDescriptor = Fields.FirstOrDefault(
                t => t.Definition.Member == member);

            if (fieldDescriptor is not null)
            {
                return fieldDescriptor;
            }

            fieldDescriptor = ObjectFieldDescriptor.New(
                Context,
                member,
                Definition.RuntimeType,
                typeof(TResolver));
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        if (member is null)
        {
            var fieldDescriptor = ObjectFieldDescriptor.New(
                Context,
                propertyOrMethod,
                Definition.RuntimeType,
                typeof(TResolver));
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        throw new ArgumentException(
            ObjectTypeDescriptor_MustBePropertyOrMethod,
            nameof(member));
    }

    public IObjectTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Definition.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public IObjectTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Definition.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IObjectTypeDescriptor Directive(string name, params ArgumentNode[] arguments)
    {
        Definition.AddDirective(name, arguments);
        return this;
    }

    public IObjectTypeDescriptor ExtendsType(Type extendsType)
    {
        Definition.ExtendsType = extendsType;
        return this;
    }

    public IObjectTypeDescriptor ExtendsType<T>()
    {
        Definition.ExtendsType = typeof(T);
        return this;
    }

    public static ObjectTypeDescriptor New(
        IDescriptorContext context) =>
        new(context);

    public static ObjectTypeDescriptor New(
        IDescriptorContext context,
        Type clrType) =>
        new(context, clrType);

    public static ObjectTypeDescriptor<T> New<T>(
        IDescriptorContext context) =>
        new(context);

    public static ObjectTypeExtensionDescriptor<T> NewExtension<T>(
        IDescriptorContext context) =>
        new(context);

    public static ObjectTypeDescriptor FromSchemaType(
        IDescriptorContext context,
        Type schemaType) =>
        new(context, schemaType)
        {
            Definition = { RuntimeType = typeof(object) }
        };

    public static ObjectTypeDescriptor From(
        IDescriptorContext context,
        ObjectTypeDefinition definition) =>
        new(context, definition);

    public static ObjectTypeDescriptor<T> From<T>(
        IDescriptorContext context,
        ObjectTypeDefinition definition) =>
        new(context, definition);
}
