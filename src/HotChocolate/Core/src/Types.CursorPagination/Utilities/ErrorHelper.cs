namespace HotChocolate.Types.Pagination;

internal static class ErrorHelper
{
    public static IError RelativeCursorsNotEnabled(
        IOutputFieldDefinition field,
        Path path)
        => ErrorBuilder.New()
            .SetMessage(
                "The field `{0}` does not allow relative cursors. "
                + "Enable the `EnableRelativeCursors` option to select them.",
                field.Name)
            .SetPath(path)
            .SetCoordinate(field.Coordinate)
            .Build();
}
