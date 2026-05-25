using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

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
        Directives = null!;
        Bind = bind;
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

        var inspector = context.TypeInspector;
        var options = context.DescriptorContext.Options;

        if (SpecifiedBy is not null)
        {
            var specifiedByTypeRef = inspector.GetTypeRef(typeof(SpecifiedByDirectiveType));
            context.Dependencies.Add(new TypeDependency(specifiedByTypeRef));
        }

        if (ApplySerializeAsToScalars
            && options.ApplySerializeAsToScalars
            && SerializationType is not ScalarSerializationType.Undefined
            && !SpecScalarNames.IsSpecScalar(Name))
        {
            var serializedAsTypeRef = inspector.GetTypeRef(typeof(SerializeAs));
            context.Dependencies.Add(new TypeDependency(serializedAsTypeRef));
            configuration.AddDirective(new SerializeAs(SerializationType, Pattern), inspector);
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

        if (configuration.SpecifiedBy is not null)
        {
            SpecifiedBy = configuration.SpecifiedBy;
        }
    }

    protected sealed override void OnBeforeCompleteMetadata(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
    {
        OnBeforeCompleteMetadata(context, (ScalarTypeConfiguration)configuration);
        base.OnBeforeCompleteMetadata(context, configuration);
    }

    protected virtual void OnBeforeCompleteMetadata(
        ITypeCompletionContext context,
        ScalarTypeConfiguration configuration)
    {
        var directiveDefinitions = configuration.GetDirectives();
        Directives = DirectiveCollection.CreateAndComplete(context, this, directiveDefinitions);
    }
}
