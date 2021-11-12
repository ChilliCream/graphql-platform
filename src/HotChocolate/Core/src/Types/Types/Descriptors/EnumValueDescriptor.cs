using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

public class EnumValueDescriptor
    : DescriptorBase<EnumValueDefinition>
    , IEnumValueDescriptor
{
    protected EnumValueDescriptor(IDescriptorContext context, object runtimeValue)
        : base(context)
    {
        if (runtimeValue is null)
        {
            throw new ArgumentNullException(nameof(runtimeValue));
        }

        Definition.Name = context.Naming.GetEnumValueName(runtimeValue);
        Definition.RuntimeValue = runtimeValue;
        Definition.Description = context.Naming.GetEnumValueDescription(runtimeValue);
        Definition.Member = context.TypeInspector.GetEnumValueMember(runtimeValue);

        if (context.Naming.IsDeprecated(runtimeValue, out var reason))
        {
            Deprecated(reason);
        }
    }

    protected EnumValueDescriptor(IDescriptorContext context, EnumValueDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected internal override EnumValueDefinition Definition { get; protected set; } = new();

    protected override void OnCreateDefinition(EnumValueDefinition definition)
    {
        if (!Definition.AttributesAreApplied && Definition.Member is not null)
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Definition.Member);
            Definition.AttributesAreApplied = true;

            if (Context.TypeInspector.IsMemberIgnored(Definition.Member))
            {
                Ignore();
            }
        }

        base.OnCreateDefinition(definition);
    }

    public IEnumValueDescriptor SyntaxNode(
        EnumValueDefinitionNode enumValueDefinition)
    {
        Definition.SyntaxNode = enumValueDefinition;
        return this;
    }

    public IEnumValueDescriptor Name(NameString value)
    {
        Definition.Name = value.EnsureNotEmpty(nameof(value));
        return this;
    }

    public IEnumValueDescriptor Description(string value)
    {
        Definition.Description = value;
        return this;
    }

    [Obsolete("Use `Deprecated`.")]
    public IEnumValueDescriptor DeprecationReason(string reason) =>
        Deprecated(reason);

    public IEnumValueDescriptor Deprecated(string reason)
    {
        if (string.IsNullOrEmpty(reason))
        {
            return Deprecated();
        }

        Definition.DeprecationReason = reason;
        return this;
    }

    public IEnumValueDescriptor Deprecated()
    {
        Definition.DeprecationReason = WellKnownDirectives.DeprecationDefaultReason;
        return this;
    }

    public IEnumValueDescriptor Ignore(bool ignore = true)
    {
        Definition.Ignore = ignore;
        return this;
    }

    public IEnumValueDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Definition.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public IEnumValueDescriptor Directive<T>()
        where T : class, new()
    {
        Definition.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IEnumValueDescriptor Directive(
        NameString name, params ArgumentNode[] arguments)
    {
        Definition.AddDirective(name, arguments);
        return this;
    }

    public static EnumValueDescriptor New(
        IDescriptorContext context,
        object value) =>
        new EnumValueDescriptor(context, value);

    public static EnumValueDescriptor From(
        IDescriptorContext context,
        EnumValueDefinition definition) =>
        new EnumValueDescriptor(context, definition);
}
