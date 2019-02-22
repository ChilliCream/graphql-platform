using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IEnumTypeDescriptor<T>
        : IEnumTypeDescriptor
    {
        /// <summary>
        /// Associates the enum type with a syntax node
        /// of the parsed GraphQL SDL.
        /// </summary>
        /// <param name="enumTypeDefinition">
        /// The the type definition node.
        /// </param>
        new IEnumTypeDescriptor<T> SyntaxNode(
            EnumTypeDefinitionNode enumTypeDefinition);

        /// <summary>
        /// Defines the name the enum type shall have.
        /// </summary>
        /// <param name="value">
        /// The name value.
        /// </param>
        new IEnumTypeDescriptor<T> Name(
            NameString value);

        /// <summary>
        /// Defines the description that the enum type shall have.
        /// </summary>
        /// <param name="value">
        /// The description value.
        /// </param>
        new IEnumTypeDescriptor<T> Description(
            string value);

        IEnumTypeDescriptor<T> Item(T value);

        new IEnumTypeDescriptor<T> BindItems(
            BindingBehavior behavior);

        new IEnumTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class;

        new IEnumTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        new IEnumTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
