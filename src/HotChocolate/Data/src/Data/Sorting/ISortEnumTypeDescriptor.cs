using HotChocolate.Language;

namespace HotChocolate.Data.Sorting;

public interface ISortEnumTypeDescriptor
{
    /// <summary>
    /// Defines the name the enum type shall have.
    /// </summary>
    /// <param name="value">
    /// The name value.
    /// </param>
    ISortEnumTypeDescriptor Name(string value);

    /// <summary>
    /// Defines the description that the enum type shall have.
    /// </summary>
    /// <param name="value">
    /// The description value.
    /// </param>
    ISortEnumTypeDescriptor Description(string value);

    ISortEnumValueDescriptor Operation(int operation);

    ISortEnumTypeDescriptor Directive<T>(T directiveInstance) where T : class;

    ISortEnumTypeDescriptor Directive<T>() where T : class, new();

    ISortEnumTypeDescriptor Directive(string name, params ArgumentNode[] arguments);
}
