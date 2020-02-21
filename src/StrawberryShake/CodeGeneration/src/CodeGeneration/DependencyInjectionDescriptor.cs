using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    public class DependencyInjectionDescriptor
        : ICodeDescriptor
    {
        public DependencyInjectionDescriptor(
            string name,
            string @namespace,
            string clientName,
            string clientTypeName,
            string clientInterfaceTypeName,
            bool enableSubscriptions,
            IReadOnlyList<string> valueSerializers,
            IReadOnlyList<string> resultParsers)
        {
            Name = name;
            Namespace = @namespace;
            ClientName = clientName;
            ClientTypeName = clientTypeName;
            ClientInterfaceTypeName = clientInterfaceTypeName;
            EnableSubscriptions = enableSubscriptions;
            ValueSerializers = valueSerializers;
            ResultParsers = resultParsers;
        }

        public string Name { get; }

        public string Namespace { get; }

        public string ClientName { get; }

        public string ClientTypeName { get; }

        public string ClientInterfaceTypeName { get; }

        public bool EnableSubscriptions { get; }

        public IReadOnlyList<string> ValueSerializers { get; }

        public IReadOnlyList<string> ResultParsers { get; }

    }
}
