using System;
using System.Collections.Generic;

namespace HotChocolate;

public class MutationResultTests
{
    [Fact]
    public void Union_1_Result_Null()
    {
        var result = new MutationResult<string>(default(string));
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_1_Cast_Result_Null()
    {
        MutationResult<string> result = default(string)!;
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_1_Cast_Result()
    {
        MutationResult<string> result = "abc";

        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_1_Result()
    {
        var result = new MutationResult<string>("abc");

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_1_Error_Null()
    {
        void Error() => new MutationResult<string>(default(object)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_1_Error()
    {
        var error = new object();
        var result = new MutationResult<string>(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_1_Errors_Null()
    {
        void Error() => new MutationResult<string>(default(object[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_1_Errors()
    {
        var error = new object();
        var result = new MutationResult<string>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_1_Errors_Empty()
    {
        void Error() => new MutationResult<string>(Array.Empty<object>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_1_Errors_Element_Null()
    {
        void Error() => new MutationResult<string>(default(object)!, default(object)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_1_Errors_Enum_Null()
    {
        void Error() => new MutationResult<string>(default(List<object>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_1_Errors_Enum()
    {
        var error = new object();
        var result = new MutationResult<string>(new List<object> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_1_Errors_Enum_Empty()
    {
        void Error() => new MutationResult<string>(new List<object>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_1_Errors_Enum_Element_Null()
    {
        void Error() => new MutationResult<string>(new List<object> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_2_Result_Null()
    {
        var result = new MutationResult<string, ErrorObj1>(default(string));
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_2_Cast_Result_Null()
    {
        MutationResult<string, ErrorObj1> result = default(string)!;
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_2_Cast_Result()
    {
        MutationResult<string, ErrorObj1> result = "abc";

        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_2_Result()
    {
        var result = new MutationResult<string, ErrorObj1>("abc");

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_2_Cast_Error()
    {
        var error = new ErrorObj1();
        MutationResult<string, ErrorObj1> result = error;

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_2_Cast_Error_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1> result = default(ErrorObj1);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_2_Error_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1>(default(ErrorObj1)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_2_Error()
    {
        var error = new ErrorObj1();
        var result = new MutationResult<string, ErrorObj1>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_2_Errors_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1>(default(ErrorObj1[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_2_Errors()
    {
        var error = new ErrorObj1();
        var result = new MutationResult<string, ErrorObj1>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_2_Errors_Empty()
    {
        void Error() => new MutationResult<string, ErrorObj1>(Array.Empty<ErrorObj1>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_2_Errors_Element_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1>(
            default(ErrorObj1)!,
            default(ErrorObj1)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_2_Errors_Enum_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1>(default(List<ErrorObj1>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_2_Errors_Enum()
    {
        var error = new ErrorObj1();
        var result = new MutationResult<string, ErrorObj1>(new List<ErrorObj1> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_2_Errors_Enum_Empty()
    {
        void Error() => new MutationResult<string, ErrorObj1>(new List<ErrorObj1>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_2_Errors_Enum_Element_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1>(new List<ErrorObj1> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Result_Null()
    {
        var result = new MutationResult<string, ErrorObj1, ErrorObj2>(default(string));
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_3_Cast_Result_Null()
    {
        MutationResult<string, ErrorObj1, ErrorObj2> result = default(string)!;
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_3_Cast_Result()
    {
        MutationResult<string, ErrorObj1, ErrorObj2> result = "abc";

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_3_Result()
    {
        var result = new MutationResult<string, ErrorObj1, ErrorObj2>("abc");

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_3_Error()
    {
        var error = new object();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_3_Errors_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(default(object[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_3_Errors()
    {
        var error = new object();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_3_Errors_Empty()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(Array.Empty<object>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Errors_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2>(default(object)!, default(object)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Errors_Enum_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(default(List<object>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_3_Errors_Enum()
    {
        var error = new object();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2>(new List<object> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_3_Errors_Enum_Empty()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(new List<object>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Errors_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2>(new List<object> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Cast_Error_1()
    {
        var error = new ErrorObj1();
        MutationResult<string, ErrorObj1, ErrorObj2> result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_3_Cast_Error_1_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2> result = default(ErrorObj1);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_3_Error_1_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(default(ErrorObj1)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_3_Error_1()
    {
        var error = new ErrorObj1();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_3_Errors_1_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(default(ErrorObj1[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_3_Errors_1()
    {
        var error = new ErrorObj1();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_3_Errors_1_Empty()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(Array.Empty<ErrorObj1>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Errors_1_Element_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(
            default(ErrorObj1)!,
            default(ErrorObj1)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Errors_1_Enum_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(default(List<ErrorObj1>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_3_Errors_1_Enum()
    {
        var error = new ErrorObj1();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2>(new List<ErrorObj1> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_3_Errors_1_Enum_Empty()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(new List<ErrorObj1>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Errors_1_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2>(new List<ErrorObj1> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Cast_Error_2()
    {
        var error = new ErrorObj2();
        MutationResult<string, ErrorObj1, ErrorObj2> result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_3_Cast_Error_2_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2> result = default(ErrorObj2);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_3_Error_2_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(default(ErrorObj2)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_3_Error_2()
    {
        var error = new ErrorObj2();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_3_Errors_2_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(default(ErrorObj2[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_3_Errors_2()
    {
        var error = new ErrorObj2();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_3_Errors_2_Empty()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(Array.Empty<ErrorObj2>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Errors_2_Element_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(
            default(ErrorObj2)!,
            default(ErrorObj2)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Errors_2_Enum_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(default(List<ErrorObj2>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_3_Errors_2_Enum()
    {
        var error = new ErrorObj2();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2>(new List<ErrorObj2> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_3_Errors_2_Enum_Empty()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2>(new List<ErrorObj2>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_3_Errors_2_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2>(new List<ErrorObj2> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Result_Null()
    {
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(default(string));
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_4_Cast_Result_Null()
    {
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3> result = default(string)!;
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_4_Cast_Result()
    {
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3> result = "abc";

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_4_Result()
    {
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>("abc");

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_4_Error()
    {
        var error = new object();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_4_Errors_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(default(object[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Errors()
    {
        var error = new object();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_4_Errors_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(Array.Empty<object>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                default(object)!,
                default(object)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(default(List<object>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Errors_Enum()
    {
        var error = new object();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                new List<object> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_4_Errors_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(new List<object>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                new List<object> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Cast_Error_1()
    {
        var error = new ErrorObj1();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3> result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_4_Cast_Error_1_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3> result = default(ErrorObj1);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Error_1_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(default(ErrorObj1)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Error_1()
    {
        var error = new ErrorObj1();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_4_Errors_1_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(default(ErrorObj1[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Errors_1()
    {
        var error = new ErrorObj1();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_4_Errors_1_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                Array.Empty<ErrorObj1>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_1_Element_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
            default(ErrorObj1)!,
            default(ErrorObj1)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_1_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                default(List<ErrorObj1>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Errors_1_Enum()
    {
        var error = new ErrorObj1();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                new List<ErrorObj1> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_4_Errors_1_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(new List<ErrorObj1>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_1_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                new List<ErrorObj1> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Cast_Error_2()
    {
        var error = new ErrorObj2();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3> result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_4_Cast_Error_2_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3> result = default(ErrorObj2);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Error_2_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(default(ErrorObj2)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Error_2()
    {
        var error = new ErrorObj2();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_4_Errors_2_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(default(ErrorObj2[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Errors_2()
    {
        var error = new ErrorObj2();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_4_Errors_2_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                Array.Empty<ErrorObj2>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_2_Element_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
            default(ErrorObj2)!,
            default(ErrorObj2)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_2_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                default(List<ErrorObj2>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Errors_2_Enum()
    {
        var error = new ErrorObj2();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                new List<ErrorObj2> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_4_Errors_2_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(new List<ErrorObj2>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_2_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                new List<ErrorObj2> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Cast_Error_3()
    {
        var error = new ErrorObj3();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3> result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_4_Cast_Error_3_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3> result = default(ErrorObj3);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Error_3_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(default(ErrorObj3)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Error_3()
    {
        var error = new ErrorObj3();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_4_Errors_3_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(default(ErrorObj3[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Errors_3()
    {
        var error = new ErrorObj3();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_4_Errors_3_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                Array.Empty<ErrorObj3>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_3_Element_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
            default(ErrorObj3)!,
            default(ErrorObj3)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_3_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                default(List<ErrorObj3>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_4_Errors_3_Enum()
    {
        var error = new ErrorObj3();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                new List<ErrorObj3> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_4_Errors_3_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(new List<ErrorObj3>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_4_Errors_3_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3>(
                new List<ErrorObj3> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Result_Null()
    {
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
            default(string));
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_5_Cast_Result_Null()
    {
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4> result =
            default(string)!;
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_5_Cast_Result()
    {
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4> result = "abc";

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_5_Result()
    {
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>("abc");

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_5_Error()
    {
        var error = new object();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_5_Errors_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(object[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Errors()
    {
        var error = new object();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_5_Errors_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                Array.Empty<object>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(object)!,
                default(object)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(List<object>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Errors_Enum()
    {
        var error = new object();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<object> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_5_Errors_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<object>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<object> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Cast_Error_1()
    {
        var error = new ErrorObj1();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4> result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_5_Cast_Error_1_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4> result =
                default(ErrorObj1);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Error_1_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(ErrorObj1)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Error_1()
    {
        var error = new ErrorObj1();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_5_Errors_1_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(ErrorObj1[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Errors_1()
    {
        var error = new ErrorObj1();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_5_Errors_1_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                Array.Empty<ErrorObj1>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_1_Element_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
            default(ErrorObj1)!,
            default(ErrorObj1)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_1_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(List<ErrorObj1>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Errors_1_Enum()
    {
        var error = new ErrorObj1();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj1> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_5_Errors_1_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj1>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_1_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj1> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Cast_Error_2()
    {
        var error = new ErrorObj2();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4> result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_5_Cast_Error_2_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4> result =
                default(ErrorObj2);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Error_2_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(ErrorObj2)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Error_2()
    {
        var error = new ErrorObj2();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_5_Errors_2_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(ErrorObj2[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Errors_2()
    {
        var error = new ErrorObj2();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_5_Errors_2_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                Array.Empty<ErrorObj2>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_2_Element_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
            default(ErrorObj2)!,
            default(ErrorObj2)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_2_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(List<ErrorObj2>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Errors_2_Enum()
    {
        var error = new ErrorObj2();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj2> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_5_Errors_2_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj2>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_2_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj2> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Cast_Error_3()
    {
        var error = new ErrorObj3();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4> result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_5_Cast_Error_3_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4> result =
                default(ErrorObj3);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Error_3_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(ErrorObj3)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Error_3()
    {
        var error = new ErrorObj3();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_5_Errors_3_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(ErrorObj3[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Errors_3()
    {
        var error = new ErrorObj3();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_5_Errors_3_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                Array.Empty<ErrorObj3>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_3_Element_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
            default(ErrorObj3)!,
            default(ErrorObj3)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_3_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(List<ErrorObj3>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Errors_3_Enum()
    {
        var error = new ErrorObj3();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj3> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_5_Errors_3_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj3>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_3_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj3> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Cast_Error_4()
    {
        var error = new ErrorObj4();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4> result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_5_Cast_Error_4_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4> result =
                default(ErrorObj4);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Error_4_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(ErrorObj4)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Error_4()
    {
        var error = new ErrorObj4();
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_5_Errors_4_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(ErrorObj4[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Errors_4()
    {
        var error = new ErrorObj4();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(error, error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_5_Errors_4_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                Array.Empty<ErrorObj4>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_4_Element_Null()
    {
        void Error() => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
            default(ErrorObj4)!,
            default(ErrorObj4)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_4_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                default(List<ErrorObj4>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_5_Errors_4_Enum()
    {
        var error = new ErrorObj4();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj4> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_5_Errors_4_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj4>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_5_Errors_4_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4>(
                new List<ErrorObj4> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Result_Null()
    {
        var result = new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
            default(string));

        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_6_Cast_Result_Null()
    {
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5> result =
            default(string)!;

        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Union_6_Cast_Result()
    {
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>
            result = "abc";

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Result()
    {
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                "abc");

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Value);
        Assert.Equal(result.Value, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Error()
    {
        var error = new object();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Errors_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(object[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors()
    {
        var error = new object();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error,
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                Array.Empty<object>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(object)!,
                default(object)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(List<object>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors_Enum()
    {
        var error = new object();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<object> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<object>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<object> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Cast_Error_1()
    {
        var error = new ErrorObj1();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>
            result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Cast_Error_1_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5> result =
                default(ErrorObj1);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Error_1_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj1)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Error_1()
    {
        var error = new ErrorObj1();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Errors_1_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj1[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors_1()
    {
        var error = new ErrorObj1();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error,
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_1_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                Array.Empty<ErrorObj1>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_1_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj1)!,
                default(ErrorObj1)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_1_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(List<ErrorObj1>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors_1_Enum()
    {
        var error = new ErrorObj1();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj1> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_1_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj1>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_1_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj1> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Cast_Error_2()
    {
        var error = new ErrorObj2();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>
            result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Cast_Error_2_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5> result =
                default(ErrorObj2);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Error_2_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj2)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Error_2()
    {
        var error = new ErrorObj2();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Errors_2_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj2[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors_2()
    {
        var error = new ErrorObj2();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error,
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_2_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                Array.Empty<ErrorObj2>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_2_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj2)!,
                default(ErrorObj2)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_2_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(List<ErrorObj2>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors_2_Enum()
    {
        var error = new ErrorObj2();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj2> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_2_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj2>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_2_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj2> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Cast_Error_3()
    {
        var error = new ErrorObj3();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>
            result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Cast_Error_3_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5> result =
                default(ErrorObj3);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Error_3_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj3)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Error_3()
    {
        var error = new ErrorObj3();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Errors_3_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj3[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors_3()
    {
        var error = new ErrorObj3();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error,
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_3_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                Array.Empty<ErrorObj3>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_3_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj3)!,
                default(ErrorObj3)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_3_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(List<ErrorObj3>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors_3_Enum()
    {
        var error = new ErrorObj3();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj3> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_3_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj3>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_3_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj3> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Cast_Error_4()
    {
        var error = new ErrorObj4();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>
            result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Cast_Error_4_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5> result =
                default(ErrorObj4);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Error_4_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj4)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Error_4()
    {
        var error = new ErrorObj4();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Errors_4_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj4[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors_4()
    {
        var error = new ErrorObj4();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error,
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_4_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                Array.Empty<ErrorObj4>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_4_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj4)!,
                default(ErrorObj4)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_4_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(List<ErrorObj4>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors_4_Enum()
    {
        var error = new ErrorObj4();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj4> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_4_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj4>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_4_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj4> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Cast_Error_5()
    {
        var error = new ErrorObj5();
        MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>
            result = error;

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, err => Assert.Equal(error, err));
        Assert.Equal(result.Errors, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Cast_Error_5_Null()
    {
        void Error()
        {
            MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5> result =
                default(ErrorObj5);
        }

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Error_5_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj5)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Error_5()
    {
        var error = new ErrorObj5();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(result.Errors!, obj => Assert.Equal(error, obj));
        Assert.Equal(result.Errors!, ((IMutationResult)result).Value);
    }

    [Fact]
    public void Union_6_Errors_5_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj5[])!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors_5()
    {
        var error = new ErrorObj5();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                error,
                error);

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_5_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                Array.Empty<ErrorObj5>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_5_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(ErrorObj5)!,
                default(ErrorObj5)!);

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_5_Enum_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                default(List<ErrorObj5>)!);

        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Union_6_Errors_5_Enum()
    {
        var error = new ErrorObj5();
        var result =
            new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj5> { error, error, });

        Assert.False(result.IsSuccess);
        Assert.Collection(
            result.Errors!,
            obj => Assert.Equal(error, obj),
            obj => Assert.Equal(error, obj));
    }

    [Fact]
    public void Union_6_Errors_5_Enum_Empty()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj5>());

        Assert.Throws<ArgumentException>(Error);
    }

    [Fact]
    public void Union_6_Errors_5_Enum_Element_Null()
    {
        void Error()
            => new MutationResult<string, ErrorObj1, ErrorObj2, ErrorObj3, ErrorObj4, ErrorObj5>(
                new List<ErrorObj5> { default, });

        Assert.Throws<ArgumentException>(Error);
    }

    public class ErrorObj1;

    public class ErrorObj2;

    public class ErrorObj3;

    public class ErrorObj4;

    public class ErrorObj5;
}
