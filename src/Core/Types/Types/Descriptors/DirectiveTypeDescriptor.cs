using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class DirectiveTypeDescriptor
        : DescriptorBase<DirectiveTypeDefinition>
        , IDirectiveTypeDescriptor
    {
        public DirectiveTypeDescriptor(
            IDescriptorContext context,
            Type clrType)
            : base(context)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Definition.ClrType = clrType;
            Definition.Name = context.Naming.GetTypeName(
                clrType, TypeKind.Directive);
            Definition.Description = context.Naming.GetTypeDescription(
                clrType, TypeKind.Directive);
        }

        public DirectiveTypeDescriptor(
            IDescriptorContext context,
            NameString name)
            : base(context)
        {
            Definition.ClrType = typeof(object);
            Definition.Name = name.EnsureNotEmpty(nameof(name));
        }

        protected override DirectiveTypeDefinition Definition { get; } =
            new DirectiveTypeDefinition();

        protected ICollection<DirectiveArgumentDescriptor> Arguments { get; } =
            new List<DirectiveArgumentDescriptor>();

        protected override void OnCreateDefinition(
            DirectiveTypeDefinition definition)
        {
            var arguments =
                new Dictionary<NameString, DirectiveArgumentDefinition>();
            var handledMembers = new HashSet<PropertyInfo>();

            FieldDescriptorUtilities.AddExplicitFields(
                Arguments.Select(t => t.CreateDefinition()),
                f => f.Property,
                arguments,
                handledMembers);

            OnCompleteArguments(arguments, handledMembers);

            definition.Arguments.AddRange(arguments.Values);
        }

        protected virtual void OnCompleteArguments(
            IDictionary<NameString, DirectiveArgumentDefinition> arguments,
            ISet<PropertyInfo> handledProperties)
        {
        }

        public IDirectiveTypeDescriptor SyntaxNode(
            DirectiveDefinitionNode directiveDefinitionNode)
        {
            Definition.SyntaxNode = directiveDefinitionNode;
            return this;
        }

        public IDirectiveTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IDirectiveTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public IDirectiveArgumentDescriptor Argument(NameString value)
        {
            var descriptor = new DirectiveArgumentDescriptor(
                Context,
                value.EnsureNotEmpty(nameof(value)));
            Arguments.Add(descriptor);
            return descriptor;
        }

        public IDirectiveTypeDescriptor Location(DirectiveLocation value)
        {
            var values = Enum.GetValues(typeof(DirectiveLocation));
            foreach (DirectiveLocation item in values)
            {
                if (value.HasFlag(item))
                {
                    Definition.Locations.Add(item);
                }
            }
            return this;
        }

        public IDirectiveTypeDescriptor Use(DirectiveMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            Definition.MiddlewareComponents.Add(middleware);
            return this;
        }

        [Obsolete("Replace Middleware with `Use`.")]
        public IDirectiveTypeDescriptor Middleware(
            DirectiveMiddleware middleware)
        {
            return Use(middleware);
        }

        [Obsolete("Replace Middleware with `Use`.", true)]
        public IDirectiveTypeDescriptor Middleware<T>(
            Expression<Func<T, object>> method)
        {
            // TODO : resources
            throw new NotSupportedException("Replace Middleware with `Use`.");
        }

        [Obsolete("Replace Middleware with `Use`.", true)]
        public IDirectiveTypeDescriptor Middleware<T>(
            Expression<Action<T>> method)
        {
            // TODO : resources
            throw new NotSupportedException("Replace Middleware with `Use`.");
        }

        public IDirectiveTypeDescriptor Repeatable()
        {
            Definition.IsRepeatable = true;
            return this;
        }
    }
}
