namespace HotChocolate.Stitching.Types;

public interface ITransformationResult
{
    ISchemaDocument SubGraph { get; }
    //??? Errors { get; }
}