#nullable enable

using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static System.Reflection.BindingFlags;

namespace HotChocolate.Types.Descriptors;

public class InputObjectTypeDescriptor
    : DescriptorBase<InputObjectTypeDefinition>
    , IInputObjectTypeDescriptor
{
    private readonly List<InputFieldDescriptor> _fields = [];

    protected InputObjectTypeDescriptor(IDescriptorContext context, Type runtimeType)
        : base(context)
    {
        if (runtimeType is null)
        {
            throw new ArgumentNullException(nameof(runtimeType));
        }

        Definition.RuntimeType = runtimeType;
        Definition.Name = context.Naming.GetTypeName(
            runtimeType, TypeKind.InputObject);
        Definition.Description = context.Naming.GetTypeDescription(
            runtimeType, TypeKind.InputObject);
    }

    protected InputObjectTypeDescriptor(IDescriptorContext context)
        : base(context)
    {
        Definition.RuntimeType = typeof(object);
    }

    protected InputObjectTypeDescriptor(
        IDescriptorContext context,
        InputObjectTypeDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));

        foreach (var field in definition.Fields)
        {
            _fields.Add(InputFieldDescriptor.From(Context, field));
        }
    }

    protected internal override InputObjectTypeDefinition Definition { get; protected set; } =
        new();

    protected ICollection<InputFieldDescriptor> Fields => _fields;

    protected override void OnCreateDefinition(
        InputObjectTypeDefinition definition)
    {
        Context.Descriptors.Push(this);

        if (!Definition.AttributesAreApplied && Definition.RuntimeType != typeof(object))
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Definition.RuntimeType);
            Definition.AttributesAreApplied = true;
        }

        var fields = TypeMemHelper.RentInputFieldDefinitionMap();
        var handledMembers = TypeMemHelper.RentMemberSet();

        foreach (var fieldDescriptor in _fields)
        {
            var fieldDefinition = fieldDescriptor.CreateDefinition();

            if (!fieldDefinition.Ignore && !string.IsNullOrEmpty(fieldDefinition.Name))
            {
                fields[fieldDefinition.Name] = fieldDefinition;
            }

            if (fieldDefinition.Property is { } prop)
            {
                handledMembers.Add(prop);
            }
        }

        OnCompleteFields(fields, handledMembers);

        Definition.Fields.Clear();
        Definition.Fields.AddRange(fields.Values);

        TypeMemHelper.Return(fields);
        TypeMemHelper.Return(handledMembers);

        base.OnCreateDefinition(definition);

        Context.Descriptors.Pop();
    }

    protected void InferFieldsFromFieldBindingType(
        IDictionary<string, InputFieldDefinition> fields,
        ISet<MemberInfo> handledMembers)
    {
        if (Definition.Fields.IsImplicitBinding())
        {
            var inspector = Context.TypeInspector;
            var naming = Context.Naming;
            var type = Definition.RuntimeType;
            var members = inspector.GetMembers(type);

            foreach (var member in members)
            {
                if (member is PropertyInfo propertyInfo &&
                    (propertyInfo.CanWrite || HasConstructorParameter(type, propertyInfo)))
                {
                    var name = naming.GetMemberName(propertyInfo, MemberKind.InputObjectField);

                    if (handledMembers.Add(propertyInfo) &&
                        !fields.ContainsKey(name))
                    {
                        var descriptor = InputFieldDescriptor.New(Context, propertyInfo);

                        _fields.Add(descriptor);
                        handledMembers.Add(propertyInfo);

                        // the create definition call will trigger the OnCompleteField call
                        // on the field description and trigger the initialization of the
                        // fields arguments.
                        fields[name] = descriptor.CreateDefinition();
                    }
                }
            }
        }
    }

    protected virtual void OnCompleteFields(
        IDictionary<string, InputFieldDefinition> fields,
        ISet<MemberInfo> handledMembers)
    { }

    public IInputObjectTypeDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public IInputObjectTypeDescriptor Description(string value)
    {
        Definition.Description = value;
        return this;
    }

    public IInputFieldDescriptor Field(string name)
    {
        var fieldDescriptor = _fields.Find(t => t.Definition.Name.EqualsOrdinal(name));

        if (fieldDescriptor is not null)
        {
            return fieldDescriptor;
        }

        fieldDescriptor = new InputFieldDescriptor(Context, name);
        _fields.Add(fieldDescriptor);
        return fieldDescriptor;
    }

    public IInputObjectTypeDescriptor Directive<T>(T directive)
        where T : class
    {
        Definition.AddDirective(directive, Context.TypeInspector);
        return this;
    }

    public IInputObjectTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Definition.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IInputObjectTypeDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        Definition.AddDirective(name, arguments);
        return this;
    }

    public static InputObjectTypeDescriptor New(IDescriptorContext context) => new(context);

    public static InputObjectTypeDescriptor New(IDescriptorContext context, Type clrType)
        => new(context, clrType);

    public static InputObjectTypeDescriptor<T> New<T>(IDescriptorContext context) => new(context);

    public static InputObjectTypeDescriptor FromSchemaType(
        IDescriptorContext context,
        Type schemaType)
    {
        var descriptor = New(context, schemaType);
        descriptor.Definition.RuntimeType = typeof(object);
        return descriptor;
    }

    public static InputObjectTypeDescriptor From(
        IDescriptorContext context,
        InputObjectTypeDefinition definition)
        => new(context, definition);

    public static InputObjectTypeDescriptor<T> From<T>(
        IDescriptorContext context,
        InputObjectTypeDefinition definition)
        => new(context, definition);

    /// <summary>
    /// Gets a value indicating whether the specified type contains a constructor parameter with the
    /// same (case-insensitive) name and type as the specified property.
    /// </summary>
    /// <param name="type">The type to check for a matching constructor parameter.</param>
    /// <param name="property">The property to compare with constructor parameters.</param>
    /// <returns>
    /// <c>true</c> if a matching constructor parameter exists; otherwise, <c>false</c>.
    /// </returns>
    private static bool HasConstructorParameter(Type type, PropertyInfo property)
    {
        return type.GetConstructors(NonPublic | Public | Instance).Any(
            c => c.GetParameters().Any(
                p => p.Name.EqualsInvariantIgnoreCase(property.Name) &&
                    p.ParameterType == property.PropertyType));
    }
}
