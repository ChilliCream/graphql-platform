using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Scalar types represent primitive leaf values in a GraphQL type system.
/// GraphQL responses take the form of a hierarchical tree;
/// the leaves on these trees are GraphQL scalars.
/// </summary>
public abstract partial class ScalarType
{
    private ITypeConverter _converter = null!;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="T:HotChocolate.Types.ScalarType"/> class.
    /// </summary>
    /// <param name="name">
    /// The unique type name.
    /// </param>
    /// <param name="bind">
    /// Defines if this scalar binds implicitly to its runtime type or
    /// if it has to be explicitly assigned to it.
    /// </param>
    protected ScalarType(string name, BindingBehavior bind = BindingBehavior.Explicit)
    {
        Name = name.EnsureGraphQLName();
        Bind = bind;

        Directives = null!;
    }

    protected override ScalarTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        var descriptor = ScalarTypeDescriptor.New(context.DescriptorContext, Name, Description, GetType());
        Configure(descriptor);
        return descriptor.CreateConfiguration();
    }

    protected virtual void Configure(IScalarTypeDescriptor descriptor) { }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        ScalarTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);

        if (SpecifiedBy is not null)
        {
            var inspector = context.TypeInspector;
            var specifiedByTypeRef = inspector.GetTypeRef(typeof(SpecifiedByDirectiveType));
            context.Dependencies.Add(new TypeDependency(specifiedByTypeRef));
        }

        if (configuration.HasDirectives)
        {
            foreach (var directive in configuration.Directives)
            {
                context.Dependencies.Add(new TypeDependency(directive.Type, TypeDependencyFulfilled.Completed));
            }
        }
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        ScalarTypeConfiguration configuration)
    {
        _converter = context.DescriptorContext.TypeConverter;
        var directiveDefinitions = configuration.GetDirectives();
        Directives = DirectiveCollection.CreateAndComplete(context, this, directiveDefinitions);

        if(configuration.SpecifiedBy is not null)
        {
            SpecifiedBy = configuration.SpecifiedBy;
        }
    }
}
