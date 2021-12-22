using System;
using static HotChocolate.Properties.AbstractionResources;

namespace HotChocolate.Abstractions;

internal static class ThrowHelper
{
    public static Exception SchemaCoordinate_MemberNameCannotBeSetOnADirectiveCoordinate(
        string argumentName) =>
        throw new ArgumentException(
            ThrowHelper_SchemaCoordinate_MemberNameCannotBeSetOnADirectiveCoordinate,
            argumentName);

    public static Exception SchemaCoordinate_ArgumentNameCannotBeSetWithoutMemberName(
        string argumentName) =>
        throw new ArgumentException(
            ThrowHelper_SchemaCoordinate_ArgumentNameCannotBeSetWithoutMemberName,
            argumentName);
}
