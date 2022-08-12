using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Execution;

internal readonly struct ArgumentContext
{
    private readonly List<Item> _items;

    private ArgumentContext(List<Item> items)
    {
        _items = items;
    }

    public ArgumentContext Push(IReadOnlyList<Argument> arguments)
    {
        var items = new List<Item>();

        foreach (var argument in arguments)
        {
            items.Add(new Item(0, argument));
        }

        if (_items is not null)
        {
            foreach (var currentItem in _items)
            {
                if (currentItem.Level > 0)
                {
                    continue;
                }

                var add = true;

                foreach (var newItem in items)
                {
                    if (currentItem.Argument.Name.EqualsOrdinal(newItem.Argument.Name))
                    {
                        add = false;
                        break;
                    }
                }

                if (add)
                {
                    items.Add(new Item(currentItem.Level + 1, currentItem.Argument));
                }
            }
        }

        return new ArgumentContext(items);
    }

    private readonly struct Item
    {
        public Item(int level, Argument argument)
        {
            Level = level;
            Argument = argument;
        }

        public int Level { get; }

        public Argument Argument { get; }
    }
}
