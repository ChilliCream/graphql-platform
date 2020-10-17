using System;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Utilities
{
    internal static class ThrowHelper
    {
        public static ArgumentException String_NullOrEmpty(
            string parameterName) =>
            new ArgumentException(
                $"'{parameterName}' cannot be null or empty",
                parameterName);

        public static GraphQLException EventMessage_InvalidCast(
            Type expectedType,
            Type messageType) =>
            new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(
                        "The event message is of the type `{0}` and cannot be casted to `{1}.`",
                        messageType.FullName,
                        expectedType.FullName)
                    .Build());

        public static GraphQLException EventMessage_NotFound() =>
            new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("There is no event message on the context.")
                    .Build());

        public static SchemaException SubscribeAttribute_MessageTypeUnspecified(
            MemberInfo member) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "You need to specify the message type on {0}.{1}. (SubscribeAttribute)",
                        member.DeclaringType.FullName,
                        member.Name)
                    .SetExtension("member", member)
                    .Build());

        public static SchemaException SubscribeAttribute_TopicTypeUnspecified(
            MemberInfo member) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "You need to specify the topic type on {0}.{1}. (SubscribeAttribute)",
                        member.DeclaringType.FullName,
                        member.Name)
                    .SetExtension("member", member)
                    .Build());

        public static SchemaException SubscribeAttribute_TopicOnParameterAndMethod(
            MemberInfo member) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The topic is declared multiple times on {0}.{1}. (TopicAttribute)",
                        member.DeclaringType!.FullName,
                        member.Name)
                    .SetExtension("member", member)
                    .Build());

        public static SchemaException SubscribeAttribute_SubscribeResolverNotFound(
            MemberInfo member,
            string subscribeResolverName) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "Unable to find the subscribe resolver `{2}` defined on {0}.{1}. " +
                        "The subscribe resolver bust be a method that is public, non-static " +
                        "and on the same type as the resolver. (SubscribeAttribute)",
                        member.DeclaringType!.FullName,
                        member.Name,
                        subscribeResolverName)
                    .SetExtension("member", member)
                    .Build());

        public static SchemaException Convention_UnableToCreateConvention(
            Type convention) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "Unable to create a convention instance from {0}.",
                        convention.FullName ?? convention.Name)
                    .Build());

        public static SchemaException UsePagingAttribute_NodeTypeUnknown(
            MemberInfo member) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage("The UsePaging attribute needs a valid node schema type.")
                    .SetCode("ATTR_USEPAGING_SCHEMATYPE_INVALID")
                    .SetExtension(nameof(member), member)
                    .Build());

        public static SchemaException TypeRegistrar_CreateInstanceFailed(
            Type namedSchemaType,
            Exception exception) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "Unable to create instance of type `{0}`.",
                        namedSchemaType.FullName)
                    .SetException(exception)
                    .SetExtension(nameof(namedSchemaType), namedSchemaType)
                    .Build());

        public static SchemaException TypeCompletionContext_UnableToResolveType(
            ITypeSystemObject type,
            ITypeReference typeRef) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "Unable to resolve type reference `{0}`.",
                        typeRef)
                    .SetTypeSystemObject(type)
                    .SetExtension(nameof(typeRef), typeRef)
                    .Build());

        public static SchemaException TypeInitializer_DuplicateTypeName(
            ITypeSystemObject type,
            ITypeSystemObject otherType) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        TypeResources.TypeInitializer_CompleteName_Duplicate,
                        type.Name)
                    .SetTypeSystemObject(type)
                    .SetExtension(nameof(otherType), otherType)
                    .Build());

        public static SchemaException NodeAttribute_NodeResolverNotFound(
            Type type,
            string nodeResolver) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The specified node resolver `{0}` does not exist on `{1}`.",
                        type.FullName ?? type.Name,
                        nodeResolver)
                    .Build());

        public static SchemaException NodeAttribute_IdFieldNotFound(
            Type type,
            string idField) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The specified id field `{0}` does not exist on `{1}`.",
                        type.FullName ?? type.Name,
                        idField)
                    .Build());

#nullable enable
        public static SchemaException Convention_TwoConventionsRegisteredForScope(
            Type conventionType,
            IConvention first,
            IConvention other,
            string? scope) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "There are two conventions registered for {0} in scope {1}. Only one " +
                        "convention is allowed. Use convention extensions if additional configuration " +
                        "is needed. Colliding conventions are {2} and {3}",
                        conventionType.FullName ?? conventionType.Name,
                        scope ?? "default",
                        first.GetType().FullName ?? first.GetType().Name,
                        other.GetType().FullName ?? other.GetType().Name)
                    .Build());
#nullable disable
    }
}
