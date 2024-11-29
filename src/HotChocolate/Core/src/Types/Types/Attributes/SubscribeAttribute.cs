using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using static System.Reflection.BindingFlags;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubscribeAttribute : ObjectFieldDescriptorAttribute
{
    private static readonly MethodInfo _subscribeFactory =
        typeof(SubscribeAttribute).GetMethod(nameof(SubscribeFactory), NonPublic | Static)!;

    /// <summary>
    /// The type of the message.
    /// </summary>
    public Type? MessageType { get; set; }

    /// <summary>
    /// The method that shall be used to subscribe to the pub/sub system.
    /// </summary>
    public string? With { get; set; }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        var method = (MethodInfo)member;

        if (MessageType is null)
        {
            var messageParameter =
                method.GetParameters()
                    .FirstOrDefault(t => t.IsDefined(typeof(EventMessageAttribute)));

            if (messageParameter is null)
            {
                throw SubscribeAttribute_MessageTypeUnspecified(member);
            }

            MessageType = messageParameter.ParameterType;
        }

        if (string.IsNullOrEmpty(With))
        {
            var topicString = ResolveTopicString(method);

            descriptor.Extend().OnBeforeNaming(
                (_, fieldDef) =>
                {
                    var factory = _subscribeFactory.MakeGenericMethod(MessageType);
                    factory.Invoke(null, [fieldDef, topicString,]);
                });
        }
        else
        {
            descriptor.Extend().OnBeforeNaming(
                (c, d) =>
                {
                    var subscribeResolver = member.DeclaringType?.GetMethod(
                        With!,
                        Public | NonPublic | Instance | Static);

                    if (subscribeResolver is null)
                    {
                        throw SubscribeAttribute_SubscribeResolverNotFound(member, With);
                    }

                    var map = TypeMemHelper.RentArgumentNameMap();

                    foreach (var argument in d.Arguments)
                    {
                        if (argument.Parameter is not null)
                        {
                            map[argument.Parameter] = argument.Name;
                        }
                    }

                    d.SubscribeResolver = context.ResolverCompiler.CompileSubscribe(
                        subscribeResolver,
                        d.SourceType!,
                        d.ResolverType,
                        map,
                        d.GetParameterExpressionBuilders());

                    TypeMemHelper.Return(map);
                });
        }
    }

    private static string ResolveTopicString(MethodInfo method)
    {
        if (method.IsDefined(typeof(TopicAttribute)))
        {
            return method.GetCustomAttribute<TopicAttribute>()?.Name ?? method.Name;
        }

        return method.Name;
    }

    private static void SubscribeFactory<TMessage>(
        ObjectFieldDefinition fieldDef,
        string topicString)
    {
        var arg = false;

        if (topicString.Contains('{'))
        {
            for (var i = 0; i < fieldDef.Arguments.Count; i++)
            {
                var argument = fieldDef.Arguments[i];
                var argumentPlaceholder = $"{{{argument.Name}}}";

                if (topicString.Contains(argumentPlaceholder))
                {
                    topicString = topicString.Replace(argumentPlaceholder, $"{{{i}}}");
                    arg = true;
                }
            }
        }

        if (arg)
        {
            fieldDef.SubscribeResolver = CreateArgumentSubscribeResolver<TMessage>(topicString);
        }
        else
        {
            fieldDef.SubscribeResolver = CreateSubscribeResolver<TMessage>(topicString);
        }
    }

    private static SubscribeResolverDelegate CreateSubscribeResolver<TMessage>(
        string topicString)
    {
        return async ctx =>
        {
            var ct = ctx.RequestAborted;
            var receiver = ctx.Service<ITopicEventReceiver>();
            return await receiver.SubscribeAsync<TMessage>(
                    topicString,
                    null,
                    null,
                    ct)
                .ConfigureAwait(false);
        };
    }

    private static SubscribeResolverDelegate CreateArgumentSubscribeResolver<TMessage>(
        string topicFormatString)
    {
        return async ctx =>
        {
            var ct = ctx.RequestAborted;
            var arguments = ctx.Selection.Field.Arguments;
            var argumentValues = new object[arguments.Count];

            // first we capture the argument values.
            for (var i = 0; i < arguments.Count; i++)
            {
                argumentValues[i] = ctx.ArgumentValue<object>(arguments[i].Name);
            }

            // next we create from it the topic string.
            var topicString = string.Format(topicFormatString, argumentValues);

            // last we subscribe with the topic string.
            var receiver = ctx.Service<ITopicEventReceiver>();
            return await receiver.SubscribeAsync<TMessage>(
                    topicString,
                    null,
                    null,
                    ct)
                .ConfigureAwait(false);
        };
    }
}
