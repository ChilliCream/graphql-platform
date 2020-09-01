using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Scalar types represent primitive leaf values in a GraphQL type system.
    /// GraphQL responses take the form of a hierarchical tree;
    /// the leaves on these trees are GraphQL scalars.
    /// </summary>
    public abstract partial class ScalarType
    {
        private readonly ExtensionData _contextData = new ExtensionData();
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
        protected ScalarType(NameString name, BindingBehavior bind = BindingBehavior.Explicit)
        {
            Name = name.EnsureNotEmpty(nameof(name));
            Bind = bind;

            Directives = default!;
        }

        internal sealed override void Initialize(ITypeDiscoveryContext context)
        {
            context.TypeInterceptor.OnBeforeRegisterDependencies(context, null, _contextData);
            OnRegisterDependencies(context, _contextData);
            context.TypeInterceptor.OnAfterRegisterDependencies(context, null, _contextData);
            base.Initialize(context);
        }

        protected virtual void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            IDictionary<string, object?> contextData)
        {
        }

        internal sealed override void CompleteName(ITypeCompletionContext context)
        {
            context.TypeInterceptor.OnBeforeCompleteName(context, null, _contextData);
            OnCompleteName(context, _contextData);
            base.CompleteName(context);
            context.TypeInterceptor.OnAfterCompleteName(context, null, _contextData);
        }

        protected virtual void OnCompleteName(
            ITypeCompletionContext context,
            IDictionary<string, object?> contextData)
        {
        }

        internal sealed override void CompleteType(ITypeCompletionContext context)
        {
            context.TypeInterceptor.OnBeforeCompleteType(context, null, _contextData);
            OnCompleteType(context, _contextData);
            base.CompleteType(context);
            context.TypeInterceptor.OnAfterCompleteType(context, null, _contextData);
        }

        protected virtual void OnCompleteType(
            ITypeCompletionContext context,
            IDictionary<string, object?> contextData)
        {
            _converter = context.Services.GetTypeConverter();
            Directives = DirectiveCollection.CreateAndComplete(
                context, this, Array.Empty<DirectiveDefinition>());
        }
    }
}
