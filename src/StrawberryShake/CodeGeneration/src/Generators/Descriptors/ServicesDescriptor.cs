using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace StrawberryShake.Generators.Descriptors
{
    public class ServicesDescriptor
        : IServicesDescriptor
        , IHasNamespace
    {
        public ServicesDescriptor(
            string name,
            string ns,
            IClientDescriptor client,
            IReadOnlyCollection<IInputClassDescriptor> inputTypes,
            IReadOnlyCollection<IEnumDescriptor> enumTypes,
            IReadOnlyCollection<IResultParserDescriptor> resultParsers)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Namespace = ns
                ?? throw new ArgumentNullException(nameof(ns));
            Client = client
                ?? throw new ArgumentNullException(nameof(client));
            EnumTypes = enumTypes
                ?? throw new ArgumentNullException(nameof(enumTypes));
            InputTypes = inputTypes
                ?? throw new ArgumentNullException(nameof(inputTypes));
            ResultParsers = resultParsers
                ?? throw new ArgumentNullException(nameof(resultParsers));

            OperationTypes = new HashSet<OperationType>(
                client.Operations.Select(t => t.Operation.Operation));
        }

        public string Name { get; }

        public string Namespace { get; }

        public IClientDescriptor Client { get; }

        public IReadOnlyCollection<IInputClassDescriptor> InputTypes { get; }

        public IReadOnlyCollection<IEnumDescriptor> EnumTypes { get; }

        public IReadOnlyCollection<IResultParserDescriptor> ResultParsers { get; }

        public ISet<OperationType> OperationTypes { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            yield break;
        }
    }
}
