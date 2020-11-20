using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    internal class InterfaceCompletionTypeInterceptor : TypeInterceptor
    {
        private readonly Dictionary<NameString, InterfaceType> _completed = new();
        private readonly HashSet<NameString> _completed = new();
        private readonly HashSet<NameString> _completedFields = new();
        private readonly Queue<InterfaceType> _interfaces = new();
        private readonly Queue<InterfaceType> _backlog = new();

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is InterfaceType { Implements: { Count: > 0 } } type &&
                definition is InterfaceTypeDefinition typeDef)
            {



                _completed.Clear();
                _completedFields.Clear();
                _interfaces.Clear();
                _backlog.Clear();
                _backlog.Enqueue(type);

                while(_backlog.Count > 0)
                {
                    InterfaceType current = _backlog.Dequeue();

                    foreach (var interfaceType in current.Implements)
                    {
                        if (_completed.Add(interfaceType.Name))
                        {
                            _backlog.Enqueue(interfaceType);
                            typeDef.Interfaces.

                        }
                    }
                }

                foreach (var VARIABLE in COLLECTION)
                {

                }
            }
        }
    }
}
