using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Resolvers
{
    public interface IDirectiveContext
    {
        DirectiveNode Directive { get; } // todo : should we have a runtime class for directice node?

        T Argument<T>(string name);

        IReadOnlyCollection<FieldSelection> CollectFields();

        Task<T> ResolveFieldAsync<T>();
    }

    public interface IDirectiveContext<T>
        where T : class
    {
        T Directive { get; }
    }
}
