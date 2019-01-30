using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Types
{
    internal static class FieldDescriptorUtilities
    {
        internal static void DiscoverArguments(
            ICollection<ArgumentDescription> arguments,
            MemberInfo member)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            var processed = new HashSet<NameString>();

            foreach (ArgumentDescription description in arguments)
            {
                processed.Add(description.Name);
            }

            if (member is MethodInfo method)
            {
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    string argumentName = parameter.GetGraphQLName();
                    if (IsArgumentType(method, parameter)
                        && processed.Add(argumentName))
                    {
                        var argumentDescriptor = new ArgumentDescriptor(
                            argumentName, parameter.ParameterType);

                        argumentDescriptor.Description(
                            parameter.GetGraphQLDescription());

                        arguments.Add(argumentDescriptor.CreateDescription());
                    }
                }
            }
        }

        private static bool IsArgumentType(
            MemberInfo member,
            ParameterInfo parameter)
        {
            return (ArgumentHelper
                .LookupKind(parameter, member.ReflectedType) ==
                    ArgumentKind.Argument);
        }
    }
}
