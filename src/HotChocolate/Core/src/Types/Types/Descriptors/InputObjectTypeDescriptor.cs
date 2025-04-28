#nullable enable

using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static System.Reflection.BindingFlags;

namespace HotChocolate.Types.Descriptors;

public class InputObjectTypeDescriptor
    : DescriptorBase<InputObjectTypeConfiguration>
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

        Configuration.RuntimeType = runtimeType;
        Configuration.Name = context.Naming.GetTypeName(
            runtimeType, TypeKind.InputObject);
        Configuration.Description = context.Naming.GetTypeDescription(
            runtimeType, TypeKind.InputObject);
    }

    protected InputObjectTypeDescriptor(IDescriptorContext context)
        : base(context)
    {
        Configuration.RuntimeType = typeof(object);
    }

    protected InputObjectTypeDescriptor(
        IDescriptorContext context,
        InputObjectTypeConfiguration definition)
        : base(context)
    {
        Configuration = definition ?? throw new ArgumentNullException(nameof(definition));

        foreach (var field in definition.Fields)
        {
            _fields.Add(InputFieldDescriptor.From(Context, field));
        }
    }

    protected internal override InputObjectTypeConfiguration Configuration { get; protected set; } =
        new();

    protected ICollection<InputFieldDescriptor> Fields => _fields;

    protected override void OnCreateConfiguration(
        InputObjectTypeConfiguration definition)
    {
        Context.Descriptors.Push(this);

        if (!Configuration.AttributesAreApplied && Configuration.RuntimeType != typeof(object))
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Configuration.RuntimeType);
            Configuration.AttributesAreApplied = true;
        }

        var fields = TypeMemHelper.RentInputFieldConfigurationMap();
        var handledMembers = TypeMemHelper.RentMemberSet();

        foreach (var fieldDescriptor in _fields)
        {
            var fieldDefinition = fieldDescriptor.CreateConfiguration();

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

        Configuration.Fields.Clear();
        Configuration.Fields.AddRange(fields.Values);

        TypeMemHelper.Return(fields);
        TypeMemHelper.Return(handledMembers);

        base.OnCreateConfiguration(definition);

        Context.Descriptors.Pop();
    }

    protected void InferFieldsFromFieldBindingType(
        IDictionary<string, InputFieldConfiguration> fields,
        ISet<MemberInfo> handledMembers)
    {
        if (Configuration.Fields.IsImplicitBinding())
        {
            var inspector = Context.TypeInspector;
            var naming = Context.Naming;
            var type = Configuration.RuntimeType;
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
                        fields[name] = descriptor.CreateConfiguration();
                    }
                }
            }
        }
    }

    protected virtual void OnCompleteFields(
        IDictionary<string, InputFieldConfiguration> fields,
        ISet<MemberInfo> handledMembers)
    { }

    public IInputObjectTypeDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public IInputObjectTypeDescriptor Description(string value)
    {
        Configuration.Description = value;
        return this;
    }

    public IInputFieldDescriptor Field(string name)
    {
        var fieldDescriptor = _fields.Find(t => t.Configuration.Name.EqualsOrdinal(name));

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
        Configuration.AddDirective(directive, Context.TypeInspector);
        return this;
    }

    public IInputObjectTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Configuration.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IInputObjectTypeDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        Configuration.AddDirective(name, arguments);
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
        descriptor.Configuration.RuntimeType = typeof(object);
        return descriptor;
    }

    public static InputObjectTypeDescriptor From(
        IDescriptorContext context,
        InputObjectTypeConfiguration definition)
        => new(context, definition);

    public static InputObjectTypeDescriptor<T> From<T>(
        IDescriptorContext context,
        InputObjectTypeConfiguration definition)
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
