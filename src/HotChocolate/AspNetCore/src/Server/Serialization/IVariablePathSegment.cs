namespace HotChocolate.Server.Serialization;

internal interface IVariablePathSegment
{
    IVariablePathSegment? Next { get; }
}
