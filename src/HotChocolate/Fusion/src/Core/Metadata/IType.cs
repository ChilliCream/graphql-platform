namespace HotChocolate.Fusion.Metadata;

// TODO : should be called INamedType
internal interface IType
{
    string Name { get; }

    MemberBindingCollection Bindings { get; }
}
