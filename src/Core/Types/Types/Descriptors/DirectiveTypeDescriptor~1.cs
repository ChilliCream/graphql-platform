using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    public class DirectiveTypeDescriptor<T>
        : DirectiveTypeDescriptor
        , IDirectiveTypeDescriptor<T>
        , IHasClrType
    {
        protected internal DirectiveTypeDescriptor(IDescriptorContext context)
            : base(context, typeof(T))
        {
            Definition.Arguments.BindingBehavior =
                context.Options.DefaultBindingBehavior;
        }

        Type IHasClrType.ClrType => Definition.ClrType;

        protected override void OnCompleteArguments(
            IDictionary<NameString, DirectiveArgumentDefinition> arguments,
            ISet<PropertyInfo> handledProperties)
        {
            if (Definition.Arguments.IsImplicitBinding())
            {
                FieldDescriptorUtilities.AddImplicitFields(
                    this,
                    p => DirectiveArgumentDescriptor
                        .New(Context, p)
                        .CreateDefinition(),
                    arguments,
                    handledProperties);
            }

            base.OnCompleteArguments(arguments, handledProperties);
        }



        #region IDirectiveDescriptor<T>

        public new IDirectiveTypeDescriptor<T> SyntaxNode(
            DirectiveDefinitionNode directiveDefinitionNode)
        {
            base.SyntaxNode(directiveDefinitionNode);
            return this;
        }

        public new IDirectiveTypeDescriptor<T> Name(NameString value)
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
            Definition.Arguments.BindingBehavior = behavior;
            return this;
        }

        public IDirectiveTypeDescriptor<T> BindArgumentsExplicitly() =>
            BindArguments(BindingBehavior.Explicit);

        public IDirectiveTypeDescriptor<T> BindArgumentsImplicitly() =>
            BindArguments(BindingBehavior.Implicit);

        public IDirectiveArgumentDescriptor Argument(
            Expression<Func<T, object>> property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (property.ExtractMember() is PropertyInfo p)
            {
                DirectiveArgumentDescriptor descriptor =
                Arguments.FirstOrDefault(t => t.Definition.Property == p);
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

        [Obsolete("Replace Middleware with `Use`.")]
        public new IDirectiveTypeDescriptor<T> Middleware(
            DirectiveMiddleware middleware)
        {
            base.Middleware(middleware);
            return this;
        }

        [Obsolete("Replace Middleware with `Use`.", true)]
        public new IDirectiveTypeDescriptor<T> Middleware<TMiddleware>(
            Expression<Func<TMiddleware, object>> method)
        {
            base.Middleware(method);
            return this;
        }

        [Obsolete("Replace Middleware with `Use`.", true)]
        public new IDirectiveTypeDescriptor<T> Middleware<TMiddleware>(
            Expression<Action<TMiddleware>> method)
        {
            base.Middleware(method);
            return this;
        }

        public new IDirectiveTypeDescriptor<T> Repeatable()
        {
            base.Repeatable();
            return this;
        }

        #endregion
    }
}
