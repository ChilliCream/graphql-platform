using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal readonly struct DirectiveContext
        : IDirectiveContext
    {
        private readonly Func<Task<object>> _resolveFieldAsync;

        public DirectiveContext(
            IDirective directive,
            Func<Task<object>> resolveFieldAsync)
        {
            Directive = directive;
            _resolveFieldAsync = resolveFieldAsync;
        }

        public IDirective Directive { get; }

        public T Argument<T>(string name)
        {
            return Directive.GetArgument<T>(name);
        }

        public async Task<T> ResolveFieldAsync<T>()
        {
            return (T)await _resolveFieldAsync();
        }
    }
}
