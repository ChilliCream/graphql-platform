using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace HotChocolate.Types.Pagination;

public class CursorPagingHandlerTests
{
    [Fact]
    public void Range_Count_0()
    {
        // arrange
        var range = new CursorPagingRange(0, 0);

        // act
        var count = range.Count();

        // assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void Range_Count_1()
    {
        // arrange
        var range = new CursorPagingRange(0, 1);

        // act
        var count = range.Count();

        // assert
        Assert.Equal(1, count);
    }

    [Fact]
    public void Range_Count_2()
    {
        // arrange
        var range = new CursorPagingRange(2, 4);

        // act
        var count = range.Count();

        // assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ApplyPagination_EmptyList()
    {
        // arrange
        var data = Array.Empty<Foo>();

        // act
        var result = await Apply(data);

        // assert
        Assert.Empty(result.Edges);
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Default()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data);

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(3, ToFoo(result).Last().Index);
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(3), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_First_10()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, first: 10);

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(3, ToFoo(result).Last().Index);
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(3), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_First_2()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, first: 2);

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(1, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(1), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_First_20()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, first: 20);

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(3, ToFoo(result).Last().Index);
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(3), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_After_2_Default()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, ToBase64(1));

        // assert
        Assert.Equal(2, ToFoo(result).First().Index);
        Assert.Equal(3, ToFoo(result).Last().Index);
        Assert.False(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(2), result.Info.StartCursor);
        Assert.Equal(ToBase64(3), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_After_2_First_10()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, ToBase64(1), first: 10);

        // assert
        Assert.Equal(2, ToFoo(result).First().Index);
        Assert.Equal(3, ToFoo(result).Last().Index);
        Assert.False(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(2), result.Info.StartCursor);
        Assert.Equal(ToBase64(3), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_After_2_First_2()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
            Foo.Create(4),
        ];

        // act
        var result = await Apply(data, ToBase64(1), first: 2);

        // assert
        Assert.Equal(2, ToFoo(result).First().Index);
        Assert.Equal(3, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(2), result.Info.StartCursor);
        Assert.Equal(ToBase64(3), result.Info.EndCursor);
        Assert.Equal(5, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_After_2_First_20()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, ToBase64(1), first: 20);

        // assert
        Assert.Equal(2, ToFoo(result).First().Index);
        Assert.Equal(3, ToFoo(result).Last().Index);
        Assert.False(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(2), result.Info.StartCursor);
        Assert.Equal(ToBase64(3), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Before_Default()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, before: ToBase64(3));

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(2, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(2), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Before_Last_10()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, before: ToBase64(3), last: 10);

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(2, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(2), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Before_Last_2()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, before: ToBase64(3), last: 2);

        // assert
        Assert.Equal(1, ToFoo(result).First().Index);
        Assert.Equal(2, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(1), result.Info.StartCursor);
        Assert.Equal(ToBase64(2), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Before_Last_20()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, before: ToBase64(3), last: 20);

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(2, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(2), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Before_2_Default()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, before: ToBase64(2));

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(1, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(1), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Before_2_Last_10()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, before: ToBase64(2), last: 10);

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(1, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(1), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Before_3_Last_2()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
            Foo.Create(4),
        ];

        // act
        var result = await Apply(data, before: ToBase64(3), last: 2);

        // assert
        Assert.Equal(1, ToFoo(result).First().Index);
        Assert.Equal(2, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(1), result.Info.StartCursor);
        Assert.Equal(ToBase64(2), result.Info.EndCursor);
        Assert.Equal(5, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Before_2_Last_20()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, before: ToBase64(2), last: 20);

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(1, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(1), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Slice()
    {
        // arrange
        Foo[] data =
        [
            Foo.Create(0),
            Foo.Create(1),
            Foo.Create(2),
            Foo.Create(3),
        ];

        // act
        var result = await Apply(data, after: ToBase64(0), before: ToBase64(3));

        // assert
        Assert.Equal(1, ToFoo(result).First().Index);
        Assert.Equal(2, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(1), result.Info.StartCursor);
        Assert.Equal(ToBase64(2), result.Info.EndCursor);
        Assert.Equal(4, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Slice_WithFirst()
    {
        // arrange
        var data = Enumerable.Range(0, 10).Select(Foo.Create).ToArray();

        // act
        var result =
            await Apply(data, after: ToBase64(0), before: ToBase64(10), first: 2);

        // assert
        Assert.Equal(1, ToFoo(result).First().Index);
        Assert.Equal(2, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(1), result.Info.StartCursor);
        Assert.Equal(ToBase64(2), result.Info.EndCursor);
        Assert.Equal(10, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Slice_WithLast()
    {
        // arrange
        var data = Enumerable.Range(0, 10).Select(Foo.Create).ToArray();

        // act
        var result =
            await Apply(data, after: ToBase64(0), before: ToBase64(10), last: 2);

        // assert
        Assert.Equal(8, ToFoo(result).First().Index);
        Assert.Equal(9, ToFoo(result).Last().Index);
        Assert.False(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(8), result.Info.StartCursor);
        Assert.Equal(ToBase64(9), result.Info.EndCursor);
        Assert.Equal(10, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Slice_After_Last_2()
    {
        // arrange
        var data = Enumerable.Range(0, 10).Select(Foo.Create).ToArray();

        // act
        var result = await Apply(data, after: ToBase64(0), last: 2);

        // assert
        Assert.Equal(8, ToFoo(result).First().Index);
        Assert.Equal(9, ToFoo(result).Last().Index);
        Assert.False(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(8), result.Info.StartCursor);
        Assert.Equal(ToBase64(9), result.Info.EndCursor);
        Assert.Equal(10, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Slice_Before_First_2()
    {
        // arrange
        var data = Enumerable.Range(0, 10).Select(Foo.Create).ToArray();

        // act
        var result = await Apply(data, before: ToBase64(8), first: 2);

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(1, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(1), result.Info.EndCursor);
        Assert.Equal(10, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Slice_Before_OutOfBounds_RangeInReach()
    {
        // arrange
        var data = Enumerable.Range(0, 10).Select(Foo.Create).ToArray();

        // act
        var result = await Apply(data, before: ToBase64(12), last: 4);

        // assert
        Assert.Equal(8, ToFoo(result).First().Index);
        Assert.Equal(9, ToFoo(result).Last().Index);
        Assert.False(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(8), result.Info.StartCursor);
        Assert.Equal(ToBase64(9), result.Info.EndCursor);
        Assert.Equal(10, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Slice_Before_OutOfBounds_RangeOutOfReach()
    {
        // arrange
        var data = Enumerable.Range(0, 10).Select(Foo.Create).ToArray();

        // act
        var result = await Apply(data, before: ToBase64(20), last: 4);

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(10, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Slice_After_OutOfBounds_RangeInReach()
    {
        // arrange
        var data = Enumerable.Range(0, 10).Select(Foo.Create).ToArray();

        // act
        var result = await Apply(data, after: ToBase64(-2), first: 4);

        // assert
        Assert.Equal(0, ToFoo(result).First().Index);
        Assert.Equal(2, ToFoo(result).Last().Index);
        Assert.True(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(0), result.Info.StartCursor);
        Assert.Equal(ToBase64(2), result.Info.EndCursor);
        Assert.Equal(10, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Slice_After_OutOfBounds_RangeOutOfReach()
    {
        // arrange
        var data = Enumerable.Range(0, 10).Select(Foo.Create).ToArray();

        // act
        var result = await Apply(data, after: ToBase64(-20), first: 4);

        // assert
        Assert.Empty(ToFoo(result));
        Assert.True(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(10, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_Last_2()
    {
        // arrange
        var data = Enumerable.Range(0, 10).Select(Foo.Create).ToArray();

        // act
        var result = await Apply(data, last: 2);

        // assert
        Assert.Equal(8, ToFoo(result).First().Index);
        Assert.Equal(9, ToFoo(result).Last().Index);
        Assert.False(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Equal(ToBase64(8), result.Info.StartCursor);
        Assert.Equal(ToBase64(9), result.Info.EndCursor);
        Assert.Equal(10, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_Default()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data);

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_First2()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data, first: 2);

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_Last2()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data, last: 2);

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_After_2()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data, after: ToBase64(2));

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_Before_2()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data, before: ToBase64(2));

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_After_Minut2()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data, after: ToBase64(-2));

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_After_2_First2()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data, after: ToBase64(2), first: 2);

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_Before_2_First2()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data, before: ToBase64(2), first: 2);

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_After_Minus2_First2()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data, after: ToBase64(-2), first: 2);

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_After_2_Last2()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data, after: ToBase64(2), last: 2);

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.True(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_Before_2_Last2()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data, before: ToBase64(2), last: 2);

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    [Fact]
    public async Task ApplyPagination_EmptyList_After_Minus2_Last2()
    {
        // arrange
        var data = Enumerable.Empty<Foo>();

        // act
        var result = await Apply(data, after: ToBase64(-2), last: 2);

        // assert
        Assert.Empty(ToFoo(result));
        Assert.False(result.Info.HasNextPage);
        Assert.False(result.Info.HasPreviousPage);
        Assert.Null(result.Info.StartCursor);
        Assert.Null(result.Info.EndCursor);
        Assert.Equal(0, await result.GetTotalCountAsync(default));
    }

    private static string ToBase64(int i)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(i.ToString()));

    private static IEnumerable<Foo> ToFoo(Connection connection)
        => connection.Edges.Select(x => x.Node).OfType<Foo>();

    public class Foo
    {
        public Foo(int index)
        {
            Index = index;
        }

        public int Index { get; }

        public static Foo Create(int index) => new(index);
    }

    private static async ValueTask<Connection> Apply(
        IEnumerable<Foo> foos,
        string? after = default,
        string? before = default,
        int? first = default,
        int? last = default)
        => await foos.AsQueryable().ApplyCursorPaginationAsync(
            first, last, after, before);
}
