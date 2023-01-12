using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public abstract class OutputFieldDescriptorBase<TDefinition>
    : DescriptorBase<TDefinition>
    where TDefinition : OutputFieldDefinitionBase
{
    private ICollection<ArgumentDescriptor>? _arguments;

    protected OutputFieldDescriptorBase(IDescriptorContext context)
        : base(context)
    {
    }

    protected ICollection<ArgumentDescriptor> Arguments =>
        _arguments ??= new List<ArgumentDescriptor>();

    protected IReadOnlyDictionary<NameString, ParameterInfo> Parameters { get; set; } =
        ImmutableDictionary<NameString, ParameterInfo>.Empty;

    protected override void OnCreateDefinition(TDefinition definition)
    {
        base.OnCreateDefinition(definition);

        if (_arguments is not null)
        {
            foreach (ArgumentDescriptor argument in Arguments)
            {
                Definition.Arguments.Add(argument.CreateDefinition());
            }
        }
    }

    protected void SyntaxNode(FieldDefinitionNode? syntaxNode)
    {
        Definition.SyntaxNode = syntaxNode;
    }

    protected void Name(NameString name)
    {
        Definition.Name = name.EnsureNotEmpty(nameof(name));
    }

    protected void Description(string? description)
    {
        Definition.Description = description;
    }

    protected void Type<TOutputType>()
        where TOutputType : IOutputType
    {
        Type(typeof(TOutputType));
    }

    protected void Type(Type type)
    {
        Internal.ITypeInfo? typeInfo = Context.TypeInspector.CreateTypeInfo(type);

        if (typeInfo.IsSchemaType && !typeInfo.IsOutputType())
        {
            throw new ArgumentException(
                TypeResources.ObjectFieldDescriptorBase_FieldType);
        }

        Definition.SetMoreSpecificType(
            typeInfo.GetExtendedType(),
            TypeContext.Output);
    }

    protected void Type<TOutputType>(TOutputType outputType)
        where TOutputType : class, IOutputType
    {
        if (outputType is null)
        {
            throw new ArgumentNullException(nameof(outputType));
        }

        if (!outputType.IsOutputType())
        {
            throw new ArgumentException(
                TypeResources.ObjectFieldDescriptorBase_FieldType);
        }

        Definition.Type = new SchemaTypeReference(outputType);
    }

    protected void Type(ITypeNode typeNode)
    {
        if (typeNode is null)
        {
            throw new ArgumentNullException(nameof(typeNode));
        }
        Definition.SetMoreSpecificType(typeNode, TypeContext.Output);
    }

    protected void Argument(
        NameString name,
        Action<IArgumentDescriptor> argument)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(nameof(argument));
        }

        name.EnsureNotEmpty(nameof(name));

        ParameterInfo? parameter = null;
        Parameters?.TryGetValue(name, out parameter);

        ArgumentDescriptor? descriptor = parameter is null
            ? Arguments.FirstOrDefault(t => t.Definition.Name.Equals(name))
            : Arguments.FirstOrDefault(t => t.Definition.Parameter == parameter);

        if (descriptor is null && Definition.Arguments.Count > 0)
        {
            ArgumentDefinition? definition = parameter is null
                ? Definition.Arguments.FirstOrDefault(t => t.Name.Equals(name))
                : Definition.Arguments.FirstOrDefault(t => t.Parameter == parameter);

            if (definition is not null)
            {
                descriptor = ArgumentDescriptor.From(Context, definition);
            }
        }

        if (descriptor is null)
        {
            descriptor = parameter is null
                ? ArgumentDescriptor.New(Context, name)
                : ArgumentDescriptor.New(Context, parameter);
            Arguments.Add(descriptor);
        }

        argument(descriptor);
    }

    public void Deprecated(string? reason)
    {
        if (string.IsNullOrEmpty(reason))
        {
            Deprecated();
        }
        else
        {
            Definition.DeprecationReason = reason;
        }
    }

    public void Deprecated()
    {
        Definition.DeprecationReason =
            WellKnownDirectives.DeprecationDefaultReason;
    }

    protected void Ignore(bool ignore = true)
    {
        Definition.Ignore = ignore;
    }

    protected void Directive<T>(T directive)
        where T : class
    {
        Definition.AddDirective(directive, Context.TypeInspector);
    }

    protected void Directive<T>()
        where T : class, new()
    {
        Definition.AddDirective(new T(), Context.TypeInspector);
    }

    protected void Directive(
        NameString name,
        params ArgumentNode[] arguments)
    {
        Definition.AddDirective(name, arguments);
    }
}
