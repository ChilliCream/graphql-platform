using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
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
    private ITypeConverter _converter = default!;

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

        Directives = default!;
    }

    protected override ScalarTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        var descriptor = ScalarTypeDescriptor.New(context.DescriptorContext, Name, Description, GetType());
        Configure(descriptor);
        return descriptor.CreateDefinition();
    }

    protected virtual void Configure(IScalarTypeDescriptor descriptor) { }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        ScalarTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);

        if (SpecifiedBy is not null)
        {
            var inspector = context.TypeInspector;
            var specifiedByTypeRef = inspector.GetTypeRef(typeof(SpecifiedByDirectiveType));
            context.Dependencies.Add(new TypeDependency(specifiedByTypeRef));
        }

        if (definition.HasDirectives)
        {
            foreach (var directive in definition.Directives)
            {
                context.Dependencies.Add(new TypeDependency(directive.Type, TypeDependencyFulfilled.Completed));
            }
        }
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        ScalarTypeDefinition definition)
    {
        _converter = context.DescriptorContext.TypeConverter;
        var directiveDefinitions = definition.GetDirectives();
        Directives = DirectiveCollection.CreateAndComplete(context, this, directiveDefinitions);

        if(definition.SpecifiedBy is not null)
        {
            SpecifiedBy = definition.SpecifiedBy;
        }
    }
}
