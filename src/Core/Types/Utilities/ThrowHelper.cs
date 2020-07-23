using System.Reflection;

#nullable enable

namespace HotChocolate.Utilities
{
    internal static class ThrowHelper
    {
        public static SchemaException SubscribeAttribute_MessageTypeUnspecified(
            MemberInfo member) =>
            new SchemaException(SchemaErrorBuilder.New()
                .SetMessage(
                    "You need to specify the message type on {0}.{1}. (SubscribeAttribute)",
                    member.DeclaringType.FullName,
                    member.Name)
                .SetExtension("member", member)
                .Build());

        public static SchemaException SubscribeAttribute_TopicTypeUnspecified(
            MemberInfo member) =>
            new SchemaException(SchemaErrorBuilder.New()
                .SetMessage(
                    "You need to specify the topic type on {0}.{1}. (SubscribeAttribute)",
                    member.DeclaringType.FullName,
                    member.Name)
                .SetExtension("member", member)
                .Build());

        public static SchemaException SubscribeAttribute_TopicOnParameterAndMethod(
            MemberInfo member) =>
            new SchemaException(SchemaErrorBuilder.New()
                .SetMessage(
                    "The topic is declared multiple times on {0}.{1}. (TopicAttribute)",
                    member.DeclaringType!.FullName,
                    member.Name)
                .SetExtension("member", member)
                .Build());

        public static SchemaException SubscribeAttribute_SubscribeResolverNotFound(
            MemberInfo member, string subscribeResolverName) =>
            new SchemaException(SchemaErrorBuilder.New()
                .SetMessage(
                    "Unable to find the subscribe resolver `{2}` defined on {0}.{1}. " +
                    "The subscribe resolver bust be a method that is public, non-static " +
                    "and on the same type as the resolver. (SubscribeAttribute)",
                    member.DeclaringType!.FullName,
                    member.Name,
                    subscribeResolverName)
                .SetExtension("member", member)
                .Build());
    }
}
