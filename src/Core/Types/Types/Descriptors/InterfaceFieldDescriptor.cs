using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    internal class InterfaceFieldDescriptor
        : ObjectFieldDescriptorBase
        , IInterfaceFieldDescriptor
        , IDescriptionFactory<InterfaceFieldDescription>
    {
        private bool _argumentsInitialized;

        public InterfaceFieldDescriptor(NameString name)
            : base(new InterfaceFieldDescription { Name = name })
        {
        }

        public InterfaceFieldDescriptor(MemberInfo member)
            : base(new InterfaceFieldDescription())
        {
            FieldDescription.Member = member
                ?? throw new ArgumentNullException(nameof(member));

            FieldDescription.Name = member.GetGraphQLName();
            FieldDescription.Description = member.GetGraphQLDescription();
            FieldDescription.Type = member.GetOutputType();
            FieldDescription.AcquireNonNullStatus(member);
        }

        public InterfaceFieldDescriptor()
            : base(new InterfaceFieldDescription())
        {
        }

        protected new InterfaceFieldDescription FieldDescription =>
            (InterfaceFieldDescription)base.FieldDescription;

        public new InterfaceFieldDescription CreateDescription()
        {
            CompleteArguments();
            return FieldDescription;
        }

        private void CompleteArguments()
        {
            if (!_argumentsInitialized)
            {
                FieldDescriptorUtilities.DiscoverArguments(
                    FieldDescription.Arguments,
                    FieldDescription.Member);
                _argumentsInitialized = true;
            }
        }

        #region IInterfaceFieldDescriptor

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.SyntaxNode(
            FieldDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Name(
            NameString name)
        {
            Name(name);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.DeprecationReason(
            string deprecationReason)
        {
            DeprecationReason(deprecationReason);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Type<TOutputType>()
        {
            Type<TOutputType>();
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Type<TOutputType>(
            TOutputType type)
        {
            Type<TOutputType>(type);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Type(ITypeNode type)
        {
            Type(type);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Ignore()
        {
            FieldDescription.Ignored = true;
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Argument(
            NameString name, Action<IArgumentDescriptor> argument)
        {
            Argument(name, argument);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Directive<T>(
            T directive)
        {
            FieldDescription.Directives.AddDirective(directive);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Directive<T>()
        {
            FieldDescription.Directives.AddDirective(new T());
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            FieldDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
