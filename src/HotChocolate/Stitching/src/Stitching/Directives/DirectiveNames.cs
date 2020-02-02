using System.Collections.Generic;
namespace HotChocolate.Stitching
{
    internal static class DirectiveNames
    {
        public static NameString Delegate { get; } = "delegate";

        public static NameString Computed { get; } = "computed";

        public static NameString Source { get; } = "source";
    }
}
