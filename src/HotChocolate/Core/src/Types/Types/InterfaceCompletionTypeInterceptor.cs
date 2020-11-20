using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    internal class InterfaceCompletionTypeInterceptor : TypeInterceptor
    {
        public override void OnAfterCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is InterfaceType { Implements: { Count: > 0 } } type)
            {


            }

        }
    }
}
