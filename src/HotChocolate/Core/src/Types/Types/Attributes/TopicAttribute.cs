using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types
{
    public sealed class TopicAttribute
        : ObjectFieldDescriptorAttribute
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
        /// Gets ors sets the argument from which the value shall be used as topic.
        /// </summary>
        public string? ArgumentName { get; set; }

        /// <summary>
        /// Gets or sets the constant topic name that shall be used to receive messages.
        /// </summary>
        public string? TopicName { get; set; }

        public Type TopicType { get; set; } = typeof(string);

        public Type? MessageType { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            if (TopicName is null && ArgumentName is null)
            {
                throw TopicAttribute_TopicUnspecified(member);
            }

            if (TopicType is null)
            {
                if (member is MethodInfo method)
                {
                    ParameterInfo? parameter =
                        method.GetParameters()
                            .FirstOrDefault(t => t.IsDefined(typeof(EventMessageAttribute)));
                    if (parameter is { })
                    {
                        TopicType = parameter.ParameterType;
                    }
                }

                throw SubscribeAttribute_TopicTypeUnspecified(member);
            }

            if (MessageType is null)
            {
                throw SubscribeAttribute_MessageTypeUnspecified(member);
            }

            if (TopicName is { })
            {
                MethodInfo method = _constantTopic.MakeGenericMethod(MessageType);
                method.Invoke(null, new object?[] { descriptor, TopicName });
            }
            else if (ArgumentName is { })
            {
                MethodInfo method = _argumentTopic.MakeGenericMethod(TopicType, MessageType);
                method.Invoke(null, new object?[] { descriptor, ArgumentName });
            }
        }
    }
}
