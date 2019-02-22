using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IEnumTypeDescriptor
        : IFluent
    {
        /// <summary>
        /// Associates the enum type with a syntax node
        /// of the parsed GraphQL SDL.
        /// </summary>
        /// <param name="typeDefinition">
        /// The the type definition node.
        /// </param>
        IEnumTypeDescriptor SyntaxNode(
            EnumTypeDefinitionNode typeDefinition);

        /// <summary>
        /// Defines the name the enum type shall have.
        /// </summary>
        /// <param name="value">
        /// The name value.
        /// </param>
        IEnumTypeDescriptor Name(
            NameString value);

        /// <summary>
        /// Defines the description that the enum type shall have.
        /// </summary>
        /// <param name="value">
        /// The description value.
        /// </param>
        IEnumTypeDescriptor Description(
            string value);

        IEnumValueDescriptor Item<T>(
            T value);

        IEnumTypeDescriptor BindItems(
            BindingBehavior behavior);

        IEnumTypeDescriptor Directive<T>(
            T instance)
            where T : class;

        IEnumTypeDescriptor Directive<T>()
            where T : class, new();

        IEnumTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
