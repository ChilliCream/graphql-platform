using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IEnumTypeDescriptor<T>
        : IDescriptor<EnumTypeDefinition>
        , IFluent
    {
        /// <summary>
        /// Associates the enum type with a syntax node
        /// of the parsed GraphQL SDL.
        /// </summary>
        /// <param name="enumTypeDefinition">
        /// The the type definition node.
        /// </param>
        IEnumTypeDescriptor<T> SyntaxNode(
            EnumTypeDefinitionNode enumTypeDefinition);

        /// <summary>
        /// Defines the name the enum type shall have.
        /// </summary>
        /// <param name="value">
        /// The name value.
        /// </param>
        IEnumTypeDescriptor<T> Name(NameString value);

        /// <summary>
        /// Defines the description that the enum type shall have.
        /// </summary>
        /// <param name="value">
        /// The description value.
        /// </param>
        IEnumTypeDescriptor<T> Description(string value);

        IEnumValueDescriptor Item(T value);

        IEnumValueDescriptor Value(T value);

        IEnumTypeDescriptor<T> BindItems(BindingBehavior behavior);

        IEnumTypeDescriptor<T> BindValues(BindingBehavior behavior);

        /// <summary>
        /// Defines that all enum values have to be specified explicitly.
        /// </summary>
        IEnumTypeDescriptor<T> BindValuesExplicitly();

        /// <summary>
        /// Defines that all enum values shall be infered
        /// from the associated .Net type,
        /// </summary>
        IEnumTypeDescriptor<T> BindValuesImplicitly();

        IEnumTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class;

        IEnumTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        IEnumTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
