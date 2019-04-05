namespace HotChocolate.Types
{
    public interface ITypeSystemObject
        : IHasName
        , IHasDescription
        , IHasContextData
        , ITypeSystem
    {
    }

    // TODO : We need a better name for this one ... it is the marker type that really brings together all type system objects ISchemaObject .... we could also rename the ITypeSystemObject to something else .... INamedTypeSystemObject
    public interface ITypeSystem
    {

    }
}
