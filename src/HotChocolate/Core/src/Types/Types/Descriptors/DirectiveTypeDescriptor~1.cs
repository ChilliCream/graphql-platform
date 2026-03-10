using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors;

public class DirectiveTypeDescriptor<T>
    : DirectiveTypeDescriptor
    , IDirectiveTypeDescriptor<T>
    , IRuntimeTypeProvider
{
    protected internal DirectiveTypeDescriptor(IDescriptorContext context)
        : base(context, typeof(T))
    {
        Configuration.Arguments.BindingBehavior = context.Options.DefaultBindingBehavior;
    }

    protected internal DirectiveTypeDescriptor(
        IDescriptorContext context,
        DirectiveTypeConfiguration definition)
        : base(context, definition)
    {
        Configuration = definition;
    }

    Type IRuntimeTypeProvider.RuntimeType => Configuration.RuntimeType;

    protected override void OnCompleteArguments(
        IDictionary<string, DirectiveArgumentConfiguration> arguments,
        ISet<PropertyInfo> handledProperties)
    {
        if (Configuration.Arguments.IsImplicitBinding())
        {
            FieldDescriptorUtilities.AddImplicitFields(
                this,
                p => DirectiveArgumentDescriptor
                    .New(Context, p)
                    .CreateConfiguration(),
                arguments,
                handledProperties);
        }

        base.OnCompleteArguments(arguments, handledProperties);
    }

    public new IDirectiveTypeDescriptor<T> Name(string value)
    {
        base.Name(value);
        return this;
    }

    public new IDirectiveTypeDescriptor<T> Description(string value)
    {
        base.Description(value);
        return this;
    }

    public IDirectiveTypeDescriptor<T> BindArguments(
        BindingBehavior behavior)
    {
        Configuration.Arguments.BindingBehavior = behavior;
        return this;
    }

    public IDirectiveTypeDescriptor<T> BindArgumentsExplicitly() =>
        BindArguments(BindingBehavior.Explicit);

    public IDirectiveTypeDescriptor<T> BindArgumentsImplicitly() =>
        BindArguments(BindingBehavior.Implicit);

    public IDirectiveArgumentDescriptor Argument(
        Expression<Func<T, object>> property)
    {
        ArgumentNullException.ThrowIfNull(property);

        if (property.ExtractMember() is PropertyInfo p)
        {
            var descriptor =
            Arguments.FirstOrDefault(t => t.Configuration.Property == p);
            if (descriptor is { })
            {
                return descriptor;
            }

            descriptor = new DirectiveArgumentDescriptor(Context, p);
            Arguments.Add(descriptor);
            return descriptor;
        }

        throw new ArgumentException(
            TypeResources.DirectiveTypeDescriptor_OnlyProperties,
            nameof(property));
    }

    public new IDirectiveTypeDescriptor<T> Location(
        DirectiveLocation value)
    {
        base.Location(value);
        return this;
    }

    public new IDirectiveTypeDescriptor<T> Use(
        DirectiveMiddleware middleware)
    {
        base.Use(middleware);
        return this;
    }

    public new IDirectiveTypeDescriptor<T> Use<TMiddleware>()
        where TMiddleware : class
    {
        base.Use<TMiddleware>();
        return this;
    }

    public new IDirectiveTypeDescriptor<T> Use<TMiddleware>(
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        base.Use(factory);
        return this;
    }

    public new IDirectiveTypeDescriptor<T> Repeatable()
    {
        base.Repeatable();
        return this;
    }

    public new IDirectiveTypeDescriptor<T> Public()
    {
        base.Public();
        return this;
    }

    public new IDirectiveTypeDescriptor<T> Internal()
    {
        base.Internal();
        return this;
    }
}
