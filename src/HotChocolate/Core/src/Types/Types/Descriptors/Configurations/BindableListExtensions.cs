namespace HotChocolate.Types.Descriptors.Configurations;

public static class BindableListExtensions
{
    public static bool IsImplicitBinding<T>(this IBindableList<T> list)
        => list.BindingBehavior == BindingBehavior.Implicit;
}
