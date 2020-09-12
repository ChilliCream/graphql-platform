using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public class SortOperationDescriptor
        : SortOperationDescriptorBase
        , ISortOperationDescriptor
    {

        protected SortOperationDescriptor(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation)
            : base(context, name, type, operation)
        {
        }

        protected internal sealed override SortOperationDefintion Definition
        {
            get;
            protected set;
        } = new SortOperationDefintion();

        protected override void OnCreateDefinition(
            SortOperationDefintion definition)
        {
            if (Definition.Operation?.Property is { })
            {
                Context.TypeInspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.Operation.Property);
            }
            base.OnCreateDefinition(definition);
        }

        public ISortOperationDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        public new ISortOperationDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public new ISortOperationDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new ISortOperationDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new ISortOperationDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new ISortOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public static SortOperationDescriptor New(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation) =>
            new SortOperationDescriptor(context, name, type, operation);

        public static SortOperationDescriptor CreateOperation(
            PropertyInfo property,
            IDescriptorContext context)
        {
            var operation = new SortOperation(property);

            ExtendedTypeReference typeReference = context.TypeInspector.GetTypeRef(
                typeof(SortOperationKindType),
                TypeContext.Input);

            NameString name = context.Naming.GetMemberName(
                property, MemberKind.InputObjectField);

            return SortOperationDescriptor.New(
                context,
                name,
                typeReference,
                operation
            );
        }
    }
}
