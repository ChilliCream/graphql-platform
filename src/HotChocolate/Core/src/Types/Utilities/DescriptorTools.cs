using System;
using System.Reflection;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities
{
    /// <summary>
    /// This class provides helper for advanced type extension cases.
    /// </summary>
    public static class DescriptorTools
    {
        public static IArgumentDescriptor RewriteType(
            this IArgumentDescriptor descriptor,
            MemberInfo member,
            Type newNamedType)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (newNamedType is null)
            {
                throw new ArgumentNullException(nameof(newNamedType));
            }

            descriptor
                .Extend()
                .OnBeforeNaming((context, definition) =>
                {
                    definition.Type =
                        context.DescriptorContext.Inspector.GetReturnType(
                            member, newNamedType, TypeContext.Input);
                });

            return descriptor;
        }

        public static IDirectiveArgumentDescriptor RewriteType(
            this IDirectiveArgumentDescriptor descriptor,
            MemberInfo member,
            Type newNamedType)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (newNamedType is null)
            {
                throw new ArgumentNullException(nameof(newNamedType));
            }

            descriptor
                .Extend()
                .OnBeforeNaming((context, definition) =>
                {
                    definition.Type =
                        context.DescriptorContext.Inspector.GetReturnType(
                            member, newNamedType, TypeContext.Input);
                });

            return descriptor;
        }

        public static IInputFieldDescriptor RewriteType(
            this IInputFieldDescriptor descriptor,
            MemberInfo member,
            Type newNamedType)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (newNamedType is null)
            {
                throw new ArgumentNullException(nameof(newNamedType));
            }

            descriptor
                .Extend()
                .OnBeforeNaming((context, definition) =>
                {
                    definition.Type =
                        context.DescriptorContext.Inspector.GetReturnType(
                            member, newNamedType, TypeContext.Input);
                });

            return descriptor;
        }

        public static IObjectFieldDescriptor RewriteType(
            this IObjectFieldDescriptor descriptor,
            MemberInfo member,
            Type newNamedType)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (newNamedType is null)
            {
                throw new ArgumentNullException(nameof(newNamedType));
            }

            descriptor
                .Extend()
                .OnBeforeNaming((context, definition) =>
                {
                    definition.Type =
                        context.DescriptorContext.Inspector.GetReturnType(
                            member, newNamedType, TypeContext.Output);
                });

            return descriptor;
        }

        public static IInterfaceFieldDescriptor RewriteType(
            this IInterfaceFieldDescriptor descriptor,
            MemberInfo member,
            Type newNamedType)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (newNamedType is null)
            {
                throw new ArgumentNullException(nameof(newNamedType));
            }

            descriptor
                .Extend()
                .OnBeforeNaming((context, definition) =>
                {
                    definition.Type =
                        context.DescriptorContext.Inspector.GetReturnType(
                            member, newNamedType, TypeContext.Output);
                });

            return descriptor;
        }
    }
}
