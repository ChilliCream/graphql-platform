using System.Collections.Immutable;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public abstract class OutputFieldDescriptorBase<TDefinition>
    : DescriptorBase<TDefinition>
    where TDefinition : OutputFieldConfiguration
{
    private ICollection<ArgumentDescriptor>? _arguments;

    protected OutputFieldDescriptorBase(IDescriptorContext context)
        : base(context)
    {
    }

    protected ICollection<ArgumentDescriptor> Arguments
        => _arguments ??= [];

    protected IReadOnlyDictionary<string, ParameterInfo> Parameters { get; set; } =
        ImmutableDictionary<string, ParameterInfo>.Empty;

    protected override void OnCreateConfiguration(TDefinition definition)
    {
        base.OnCreateConfiguration(definition);

        if (_arguments is not null)
        {
            foreach (var argument in Arguments)
            {
                Configuration.Arguments.Add(argument.CreateConfiguration());
            }
        }
    }

    protected void Name(string name)
        => Configuration.Name = name;

    protected void Description(string? description)
        => Configuration.Description = description;

    protected void Type<TOutputType>()
        where TOutputType : IOutputType
        => Type(typeof(TOutputType));

    protected void Type(Type type)
    {
        var typeInfo = Context.TypeInspector.CreateTypeInfo(type);

        if (typeInfo.IsSchemaType && !typeInfo.IsOutputType())
        {
            throw new ArgumentException(
                TypeResources.ObjectFieldDescriptorBase_FieldType);
        }

        Configuration.SetMoreSpecificType(
            typeInfo.GetExtendedType(),
            TypeContext.Output);
    }

    protected void Type<TOutputType>(TOutputType outputType)
        where TOutputType : class, IOutputType
    {
        ArgumentNullException.ThrowIfNull(outputType);

        if (!outputType.IsOutputType())
        {
            throw new ArgumentException(
                TypeResources.ObjectFieldDescriptorBase_FieldType);
        }

        Configuration.Type = new SchemaTypeReference(outputType);
    }

    protected void Type(ITypeNode typeNode)
    {
        ArgumentNullException.ThrowIfNull(typeNode);
        Configuration.SetMoreSpecificType(typeNode, TypeContext.Output);
    }

    protected void Argument(
        string name,
        Action<IArgumentDescriptor> argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        name.EnsureGraphQLName();

        Parameters.TryGetValue(name, out var parameter);

        var descriptor = parameter is null
            ? Arguments.FirstOrDefault(t => t.Configuration.Name.EqualsOrdinal(name))
            : Arguments.FirstOrDefault(t => t.Configuration.Parameter == parameter);

        if (descriptor is null && Configuration.Arguments.Count > 0)
        {
            var definition = parameter is null
                ? Configuration.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(name))
                : Configuration.Arguments.FirstOrDefault(t => t.Parameter == parameter);

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
            Configuration.DeprecationReason = reason;
        }
    }

    public void Deprecated()
        => Configuration.DeprecationReason = DirectiveNames.Deprecated.Arguments.DefaultReason;

    protected void Ignore(bool ignore = true)
        => Configuration.Ignore = ignore;

    protected void Directive<T>(T directive)
        where T : class
        => Configuration.AddDirective(directive, Context.TypeInspector);

    protected void Directive<T>()
        where T : class, new()
        => Configuration.AddDirective(new T(), Context.TypeInspector);

    protected void Directive(string name, params ArgumentNode[] arguments)
        => Configuration.AddDirective(name, arguments);
}
