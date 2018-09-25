using System;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InterfaceFieldDescriptor
        : ObjectFieldDescriptorBase
        , IInterfaceFieldDescriptor
        , IDescriptionFactory<InterfaceFieldDescription>
    {
        public InterfaceFieldDescriptor(string name)
            : base(new InterfaceFieldDescription { Name = name })
        {
        }

        public InterfaceFieldDescriptor()
            : base(new InterfaceFieldDescription())
        {
        }

        protected new InterfaceFieldDescription FieldDescription =>
            (InterfaceFieldDescription)base.FieldDescription;

        public new InterfaceFieldDescription CreateDescription()
        {
            return FieldDescription;
        }

        #region IInterfaceFieldDescriptor

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.SyntaxNode(
            FieldDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Name(
            string name)
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

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Type(ITypeNode type)
        {
            Type(type);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Argument(
            string name, Action<IArgumentDescriptor> argument)
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
            string name,
            params ArgumentNode[] arguments)
        {
            FieldDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
