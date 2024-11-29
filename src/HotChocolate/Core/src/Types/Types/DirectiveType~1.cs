using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

public class DirectiveType<TDirective> : DirectiveType where TDirective : class
{
    private Action<IDirectiveTypeDescriptor<TDirective>>? _configure;

    public DirectiveType(Action<IDirectiveTypeDescriptor<TDirective>> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    [ActivatorUtilitiesConstructor]
    public DirectiveType()
    {
        _configure = Configure;
    }

    protected override DirectiveTypeDefinition CreateDefinition(
        ITypeDiscoveryContext context)
    {
        var descriptor = DirectiveTypeDescriptor.New<TDirective>(context.DescriptorContext);

        _configure!(descriptor);
        _configure = null;

        return descriptor.CreateDefinition();
    }

    protected virtual void Configure(IDirectiveTypeDescriptor<TDirective> descriptor)
    {
    }

    protected sealed override void Configure(IDirectiveTypeDescriptor descriptor)
        => throw new NotSupportedException();

    public new TDirective Parse(DirectiveNode directiveNode)
        => (TDirective)base.Parse(directiveNode);
}
