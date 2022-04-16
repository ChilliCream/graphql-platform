namespace HotChocolate.Stitching.Types.Attempt1.Wip;

public interface ITransformationResult
{
    ISchemaDocument SubGraph { get; }
    //??? Errors { get; }
}