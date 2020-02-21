using System;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class SubscribeAttribute
        : Attribute
    {
        public string? FieldName { get; set; }

        public string? MemberName { get; set; }
    }
}
