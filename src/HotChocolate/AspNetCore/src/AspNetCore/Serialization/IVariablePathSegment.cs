namespace HotChocolate.AspNetCore.Serialization
{
    internal interface IVariablePathSegment
    {
        IVariablePathSegment? Next { get; }
    }
}
