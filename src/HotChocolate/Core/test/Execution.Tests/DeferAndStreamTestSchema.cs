using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

internal static class DeferAndStreamTestSchema
{
    public static async Task<IRequestExecutor> CreateAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddGlobalObjectIdentification()
            .ModifyOptions(
                o =>
                {
                    o.EnableDefer = true;
                    o.EnableStream = true;
                })
            .BuildRequestExecutorAsync();
    }

    public class Query
    {
        private readonly List<Person> _persons =
        [
            new Person(1, "Pascal"),
            new Person(2, "Rafi"),
            new Person(3, "Martin"),
            new Person(4, "Michael"),
        ];

        [NodeResolver]
        public async Task<Person> GetPersonAsync(int id)
        {
            await Task.Delay(50);
            return _persons[id - 1];
        }

        public async IAsyncEnumerable<Person> GetPersons()
        {
            foreach (var person in _persons)
            {
                await Task.Delay(50);
                yield return person;
            }
        }

        [UsePaging]
        public IEnumerable<Person> GetPersonNodes()
        {
            foreach (var person in _persons)
            {
                yield return person;
            }
        }

        public async Task<bool> Wait(int m, CancellationToken ct)
        {
            await Task.Delay(m, ct);
            return true;
        }
    }

    public class Person
    {
        private readonly string _name;

        public Person(int id, string name)
        {
            Id = id;
            _name = name;
        }

        public int Id { get; }

        public async Task<string> GetNameAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Id * 200, cancellationToken);
            return _name;
        }
    }
}
