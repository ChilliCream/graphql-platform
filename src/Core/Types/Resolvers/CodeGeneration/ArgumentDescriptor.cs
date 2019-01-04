using System;
using System.Reflection;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal class ArgumentDescriptor
        : IDelegateDescriptor
    {
        internal ArgumentDescriptor(
            NameString name,
            string variableName,
            ArgumentKind kind,
            Type type,
            ParameterInfo parameter)
        {
            Name = name;
            VariableName = variableName;

            Kind = kind;
            Type = type;
            Parameter = parameter;
        }

        /// <summary>
        /// Gets the name of the argument that is defined in the GraphQL schema.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the internal c# variable name of the argument that is used to
        /// refer to this argment in the generated c# code..
        /// </summary>
        public string VariableName { get; }

        /// <summary>
        /// Gets the parameter from which this argument is derived.
        /// </summary>
        public ParameterInfo Parameter { get; }

        /// <summary>
        /// Defines the argument kind.
        /// </summary>
        /// <returns></returns>
        public ArgumentKind Kind { get; }

        /// <summary>
        /// Gets the CLR argument type.
        /// </summary>
        public Type Type { get; }
    }
}
