using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public interface IDirectiveContext
    {
        IDirective Directive { get; } // todo : should we have a runtime class for directice node?

        T Argument<T>(string name);

        IReadOnlyCollection<FieldSelection> CollectFields();

        Task<T> ResolveFieldAsync<T>();
    }
}
