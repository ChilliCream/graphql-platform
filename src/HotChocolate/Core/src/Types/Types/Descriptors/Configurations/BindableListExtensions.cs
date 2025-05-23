namespace HotChocolate.Types.Descriptors.Configurations;

public static class BindableListExtensions
{
    public static bool IsImplicitBinding<T>(this IBindableList<T> list)
    {
        if (list is null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        return list.BindingBehavior == BindingBehavior.Implicit;
    }
}
