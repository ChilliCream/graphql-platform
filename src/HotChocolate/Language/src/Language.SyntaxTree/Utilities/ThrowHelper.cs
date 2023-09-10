using System;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language.Utilities;

internal static class ThrowHelper
{
    public static void NodeKindIsNotSupported(SyntaxKind kind) =>
        throw new NotSupportedException($"The node kind {kind} is not supported.");

    public static Exception SchemaCoordinate_MemberNameCannotBeSetOnADirectiveCoordinate(
        string argumentName) =>
        throw new ArgumentException(
            Resources.ThrowHelper_SchemaCoordinate_MemberNameCannotBeSetOnADirectiveCoordinate,
            argumentName);

    public static Exception SchemaCoordinate_ArgumentNameCannotBeSetWithoutMemberName(
        string argumentName) =>
        throw new ArgumentException(
            Resources.ThrowHelper_SchemaCoordinate_ArgumentNameCannotBeSetWithoutMemberName,
            argumentName);
}
