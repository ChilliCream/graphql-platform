namespace HotChocolate.AspNetCore.Parsers;

internal interface IVariablePathSegment
{
    IVariablePathSegment? Next { get; }

    string ToString();
}
