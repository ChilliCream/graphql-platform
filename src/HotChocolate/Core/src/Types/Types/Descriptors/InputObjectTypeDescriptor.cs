using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors;

public class InputObjectTypeDescriptor
    : DescriptorBase<InputObjectTypeDefinition>
    , IInputObjectTypeDescriptor
{
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
            Fields.Add(InputFieldDescriptor.From(Context, field));
        }
    }

    protected internal override InputObjectTypeDefinition Definition { get; protected set; } =
        new();

    protected List<InputFieldDescriptor> Fields { get; } = new();

    protected override void OnCreateDefinition(
        InputObjectTypeDefinition definition)
    {
        if (!Definition.AttributesAreApplied && Definition.RuntimeType != typeof(object))
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Definition.RuntimeType);
            Definition.AttributesAreApplied = true;
        }

        var fields = new Dictionary<string, InputFieldDefinition>();
        var handledProperties = new HashSet<PropertyInfo>();

        FieldDescriptorUtilities.AddExplicitFields(
            Fields.Select(t => t.CreateDefinition()),
            f => f.Property,
            fields,
            handledProperties);

        OnCompleteFields(fields, handledProperties);

        Definition.Fields.AddRange(fields.Values);

        base.OnCreateDefinition(definition);
    }

    protected virtual void OnCompleteFields(
        IDictionary<string, InputFieldDefinition> fields,
        ISet<PropertyInfo> handledProperties)
    {
    }

    public IInputObjectTypeDescriptor SyntaxNode(
        InputObjectTypeDefinitionNode inputObjectTypeDefinition)
    {
        Definition.SyntaxNode = inputObjectTypeDefinition;
        return this;
    }

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
        var fieldDescriptor = Fields.FirstOrDefault(t => t.Definition.Name.EqualsOrdinal(name));

        if (fieldDescriptor is not null)
        {
            return fieldDescriptor;
        }

        fieldDescriptor = new InputFieldDescriptor(Context, name);
        Fields.Add(fieldDescriptor);
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
}
