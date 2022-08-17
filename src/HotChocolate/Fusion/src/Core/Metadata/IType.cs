namespace HotChocolate.Fusion.Metadata;

internal interface IType // TODO : should be called named type
{
    string Name { get; }

    MemberBindingCollection Bindings { get; }
}
