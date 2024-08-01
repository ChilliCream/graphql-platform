namespace HotChocolate.Subscriptions.Postgres;

public class CopyOnWriteListTests
{
    [Fact]
    public void Add_Should_IncreaseItemCount_When_NewItemAdded()
    {
        // Arrange
        var cowList = new CopyOnWriteList<Item>();
        var itemToAdd = new Item(5);

        // Act
        cowList.Add(itemToAdd);

        // Assert
        Assert.Single(cowList.Items);
        Assert.Equal(itemToAdd, cowList.Items[0]);
    }

    [Fact]
    public void Add_Should_MaintainOrder_When_MultipleItemsAdded()
    {
        // Arrange
        var cowList = new CopyOnWriteList<Item>();
        var itemToAdd1 = new Item(5);
        var itemToAdd2 = new Item(7);

        // Act
        cowList.Add(itemToAdd1);
        cowList.Add(itemToAdd2);

        // Assert
        Assert.Equal(2, cowList.Items.Length);
        Assert.Equal(itemToAdd1, cowList.Items[0]);
        Assert.Equal(itemToAdd2, cowList.Items[1]);
    }

    [Fact]
    public void Add_Should_CreateNewArray_When_ItemAdded()
    {
        // Arrange
        var cowList = new CopyOnWriteList<Item>();
        cowList.Add(new Item(1));
        var initialArray = cowList.Items;

        // Act
        cowList.Add(new Item(2));

        // Assert
        Assert.NotSame(initialArray, cowList.Items);
    }

    [Fact]
    public void Remove_Should_DecreaseItemCount_When_ItemRemoved()
    {
        // Arrange
        var cowList = new CopyOnWriteList<Item>();
        var itemToAdd = new Item(5);
        var itemToRemove = new Item(7);
        cowList.Add(itemToAdd);
        cowList.Add(itemToRemove);

        // Act
        cowList.Remove(itemToRemove);

        // Assert
        Assert.Single(cowList.Items);
        Assert.DoesNotContain(itemToRemove, cowList.Items);
    }

    [Fact]
    public void Remove_Should_NotChangeItemCount_When_ItemNotInList()
    {
        // Arrange
        var cowList = new CopyOnWriteList<Item>();
        var itemToAdd1 = new Item(5);
        var itemToAdd2 = new Item(8);
        var itemToRemove = new Item(7);
        cowList.Add(itemToAdd1);
        cowList.Add(itemToAdd2);

        // Act
        cowList.Remove(itemToRemove);

        // Assert
        Assert.Equal(2, cowList.Items.Length);
        Assert.Contains(itemToAdd1, cowList.Items);
        Assert.Contains(itemToAdd2, cowList.Items);
    }

    [Fact]
    public void Remove_Should_RemoveLastItem_When_InList()
    {
        // Arrange
        var cowList = new CopyOnWriteList<Item>();
        var itemToAdd1 = new Item(5);
        var itemToRemove = new Item(7);
        cowList.Add(itemToAdd1);
        cowList.Add(itemToRemove);

        // Act
        cowList.Remove(itemToRemove);

        // Assert
        Assert.Single(cowList.Items);
        Assert.Contains(itemToAdd1, cowList.Items);
        Assert.DoesNotContain(itemToRemove, cowList.Items);
    }

    [Fact]
    public void Remove_Should_RemoveFirstItem_When_InList()
    {
        // Arrange
        var cowList = new CopyOnWriteList<Item>();
        var itemToAdd1 = new Item(5);
        var itemToRemove = new Item(7);
        cowList.Add(itemToRemove);
        cowList.Add(itemToAdd1);

        // Act
        cowList.Remove(itemToRemove);

        // Assert
        Assert.Single(cowList.Items);
        Assert.Contains(itemToAdd1, cowList.Items);
        Assert.DoesNotContain(itemToRemove, cowList.Items);
    }

    [Fact]
    public void Remove_Should_KeepArraySame_When_NonExistingItemRemoved()
    {
        // Arrange
        var cowList = new CopyOnWriteList<Item>();
        cowList.Add(new(1));
        var initialArray = cowList.Items;

        // Act
        cowList.Remove(new(2));

        // Assert
        Assert.Same(initialArray, cowList.Items);
    }

    [Fact]
    public void Items_Should_ReturnEmpty_When_ListIsEmpty()
    {
        // Arrange
        var cowList = new CopyOnWriteList<Item>();

        // Act
        var items = cowList.Items;

        // Assert
        Assert.Empty(items);
    }

    [Fact]
    public void Items_Should_ReturnExactItems_When_ListIsNotEmpty()
    {
        // Arrange
        var cowList = new CopyOnWriteList<Item>();
        var itemToAdd1 = new Item(5);
        var itemToAdd2 = new Item(7);
        cowList.Add(itemToAdd1);
        cowList.Add(itemToAdd2);

        // Act
        var items = cowList.Items;

        // Assert
        Assert.Equal(2, items.Length);
        Assert.Contains(itemToAdd1, items);
        Assert.Contains(itemToAdd2, items);
    }

    [Fact]
    public async Task Add_Should_HandleConcurrentAdds_When_MultipleThreadsAdding()
    {
        // Arrange
        var cowList = new CopyOnWriteList<Item>();
        var tasks = new List<Task>();
        var itemsToAdd = Enumerable.Range(1, 1000).Select(i => new Item(i)).ToArray();

        // Act
        foreach (var item in itemsToAdd)
        {
            tasks.Add(Task.Run(() => cowList.Add(item)));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(itemsToAdd.Length, cowList.Items.Length);
        foreach (var item in itemsToAdd)
        {
            Assert.Contains(item, cowList.Items);
        }
    }

    public class Item
    {
        public Item(int number)
        {
            Number = number;
        }

        public int Number { get; init; }
    }
}
