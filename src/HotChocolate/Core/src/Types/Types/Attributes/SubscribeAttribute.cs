using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public sealed class SubscribeAttribute : ObjectFieldDescriptorAttribute
    {
        private static readonly MethodInfo _constantTopic =
            typeof(SubscribeResolverObjectFieldDescriptorExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.IsGenericMethod && m.GetGenericArguments().Length == 1);

        private static readonly MethodInfo _argumentTopic =
            typeof(SubscribeResolverObjectFieldDescriptorExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m =>
                    m.IsGenericMethod &&
                    m.GetGenericArguments().Length == 2 &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[1].ParameterType == typeof(string));

        /// <summary>
        /// The type of the message.
        /// </summary>
        public Type? MessageType { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            var method = (MethodInfo)member;

            if (MessageType is null)
            {
                ParameterInfo? messageParameter =
                        method.GetParameters()
                            .FirstOrDefault(t => t.IsDefined(typeof(EventMessageAttribute)));

                if (messageParameter is null)
                {
                    throw SubscribeAttribute_MessageTypeUnspecified(member);
                }

                MessageType = messageParameter.ParameterType;
            }

            (string? name, string? value, Type type) topic = ResolveTopic(method);

            if (topic.value is { })
            {
                MethodInfo config = _constantTopic.MakeGenericMethod(MessageType);
                config.Invoke(null, new object?[] { descriptor, topic.value });
            }
            else
            {
                MethodInfo config = _argumentTopic.MakeGenericMethod(topic.type, MessageType);
                config.Invoke(null, new object?[] { descriptor, topic.name });
            }
        }

        private (string? name, string? value, Type type) ResolveTopic(MethodInfo method)
        {
            ParameterInfo? topicParameter =
                method.GetParameters()
                    .FirstOrDefault(t => t.IsDefined(typeof(TopicAttribute)));

            if (method.IsDefined(typeof(TopicAttribute)))
            {
                if (topicParameter is null)
                {

                }
                else
                {
                    // throw schema error
                }
            }

            if (topicParameter is { })
            {

            }

            return (null, method.Name, typeof(string));
        }
    }
}
