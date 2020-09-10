using HotChocolate.Language;

namespace HotChocolate.Data.Sorting
{
    public interface ISortEnumTypeDescriptor
    {
        /// <summary>
        /// Associates the enum type with a syntax node
        /// of the parsed GraphQL SDL.
        /// </summary>
        /// <param name="enumTypeDefinition">
        /// The the type definition node.
        /// </param>
        ISortEnumTypeDescriptor SyntaxNode(
            EnumTypeDefinitionNode enumTypeDefinition);

        /// <summary>
        /// Defines the name the enum type shall have.
        /// </summary>
        /// <param name="value">
        /// The name value.
        /// </param>
        ISortEnumTypeDescriptor Name(
            NameString value);

        /// <summary>
        /// Defines the description that the enum type shall have.
        /// </summary>
        /// <param name="value">
        /// The description value.
        /// </param>
        ISortEnumTypeDescriptor Description(
            string value);

        ISortEnumValueDescriptor Operation(int operation);

        ISortEnumTypeDescriptor Directive<T>(
            T directiveInstance)
            where T : class;

        ISortEnumTypeDescriptor Directive<T>()
            where T : class, new();

        ISortEnumTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
