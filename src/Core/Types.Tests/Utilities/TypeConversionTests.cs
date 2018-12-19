﻿using System;
using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Utilities
{
    public class TypeConversionTests
    {
        [InlineData((ushort)1, (short)1, typeof(ushort), typeof(short))]
        [InlineData((ushort)1, (int)1, typeof(ushort), typeof(int))]
        [InlineData((ushort)1, (long)1, typeof(ushort), typeof(long))]
        [InlineData((ushort)1, (uint)1, typeof(ushort), typeof(uint))]
        [InlineData((ushort)1, (ulong)1, typeof(ushort), typeof(ulong))]
        [InlineData((ushort)1, (float)1, typeof(ushort), typeof(float))]
        [InlineData((ushort)1, (double)1, typeof(ushort), typeof(double))]
        [InlineData((ushort)1, "1", typeof(ushort), typeof(string))]

        [InlineData((uint)1, (short)1, typeof(uint), typeof(short))]
        [InlineData((uint)1, (int)1, typeof(uint), typeof(int))]
        [InlineData((uint)1, (long)1, typeof(uint), typeof(long))]
        [InlineData((uint)1, (ushort)1, typeof(uint), typeof(ushort))]
        [InlineData((uint)1, (ulong)1, typeof(uint), typeof(ulong))]
        [InlineData((uint)1, (float)1, typeof(uint), typeof(float))]
        [InlineData((uint)1, (double)1, typeof(uint), typeof(double))]
        [InlineData((uint)1, "1", typeof(uint), typeof(string))]

        [InlineData((ulong)1, (short)1, typeof(ulong), typeof(short))]
        [InlineData((ulong)1, (int)1, typeof(ulong), typeof(int))]
        [InlineData((ulong)1, (long)1, typeof(ulong), typeof(long))]
        [InlineData((ulong)1, (ushort)1, typeof(ulong), typeof(ushort))]
        [InlineData((ulong)1, (uint)1, typeof(ulong), typeof(uint))]
        [InlineData((ulong)1, (float)1, typeof(ulong), typeof(float))]
        [InlineData((ulong)1, (double)1, typeof(ulong), typeof(double))]
        [InlineData((ulong)1, "1", typeof(ulong), typeof(string))]

        [InlineData((short)1, (int)1, typeof(short), typeof(int))]
        [InlineData((short)1, (long)1, typeof(short), typeof(long))]
        [InlineData((short)1, (ushort)1, typeof(short), typeof(ushort))]
        [InlineData((short)1, (uint)1, typeof(short), typeof(uint))]
        [InlineData((short)1, (ulong)1, typeof(short), typeof(ulong))]
        [InlineData((short)1, (float)1, typeof(short), typeof(float))]
        [InlineData((short)1, (double)1, typeof(short), typeof(double))]
        [InlineData((short)1, "1", typeof(short), typeof(string))]

        [InlineData((int)1, (short)1, typeof(int), typeof(short))]
        [InlineData((int)1, (long)1, typeof(int), typeof(long))]
        [InlineData((int)1, (ushort)1, typeof(int), typeof(ushort))]
        [InlineData((int)1, (uint)1, typeof(int), typeof(uint))]
        [InlineData((int)1, (ulong)1, typeof(int), typeof(ulong))]
        [InlineData((int)1, (float)1, typeof(int), typeof(float))]
        [InlineData((int)1, (double)1, typeof(int), typeof(double))]
        [InlineData((int)1, "1", typeof(int), typeof(string))]

        [InlineData((long)1, (short)1, typeof(long), typeof(short))]
        [InlineData((long)1, (int)1, typeof(long), typeof(int))]
        [InlineData((long)1, (ushort)1, typeof(long), typeof(ushort))]
        [InlineData((long)1, (uint)1, typeof(long), typeof(uint))]
        [InlineData((long)1, (ulong)1, typeof(long), typeof(ulong))]
        [InlineData((long)1, (float)1, typeof(long), typeof(float))]
        [InlineData((long)1, (double)1, typeof(long), typeof(double))]
        [InlineData((long)1, "1", typeof(long), typeof(string))]

        [InlineData((float)1.1, (short)1, typeof(float), typeof(short))]
        [InlineData((float)1.1, (int)1, typeof(float), typeof(int))]
        [InlineData((float)1.1, (long)1, typeof(float), typeof(long))]
        [InlineData((float)1.1, (ushort)1, typeof(float), typeof(ushort))]
        [InlineData((float)1.1, (uint)1, typeof(float), typeof(uint))]
        [InlineData((float)1.1, (ulong)1, typeof(float), typeof(ulong))]
        [InlineData((float)1, 1d, typeof(float), typeof(double))]
        [InlineData((float)1.1, "1.1", typeof(float), typeof(string))]

        [InlineData((double)1.1, (short)1, typeof(double), typeof(short))]
        [InlineData((double)1.1, (int)1, typeof(double), typeof(int))]
        [InlineData((double)1.1, (long)1, typeof(double), typeof(long))]
        [InlineData((double)1.1, (ushort)1, typeof(double), typeof(ushort))]
        [InlineData((double)1.1, (uint)1, typeof(double), typeof(uint))]
        [InlineData((double)1.1, (ulong)1, typeof(double), typeof(ulong))]
        [InlineData((double)1.1, (float)1.1, typeof(double), typeof(float))]
        [InlineData((double)1.1, "1.1", typeof(double), typeof(string))]

        [Theory]
        public void ConvertNumber(object input, object expectedOutput,
            Type from, Type to)
        {
            // arrange
            // act
            bool success = TypeConversion.Default.TryConvert(
                from, to, input, out object output);

            // assert
            Assert.True(success);
            Assert.Equal(to, output.GetType());
            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Convert_Int_NullableLong()
        {
            // arrange
            int source = 55;

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(int), typeof(Nullable<long>),
                source, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<long>(output);
            Assert.Equal(55L, output);
        }

        [Fact]
        public void Convert_NullableInt_NullableLong()
        {
            // arrange
            Nullable<int> source = 55;

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(Nullable<int>), typeof(Nullable<long>),
                source, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<long>(output);
            Assert.Equal(55L, output);
        }

        [Fact]
        public void Convert_NullString_NullableLong()
        {
            // arrange
            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(string), typeof(Nullable<long>),
                null, out object output);

            // assert
            Assert.True(success);
            Assert.Null(output);
        }


        [Fact]
        public void Convert_NullableLong_Int()
        {
            // arrange
            Nullable<long> source = 55;

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(Nullable<long>), typeof(int),
                source, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<int>(output);
            Assert.Equal(55, output);
        }


        [InlineData("{2d84dcd6-3439-4ebe-8427-f4b1e1730c47}")]
        [InlineData("2d84dcd6-3439-4ebe-8427-f4b1e1730c47")]
        [InlineData("2d84dcd634394ebe8427f4b1e1730c47")]
        [Theory]
        public void Convert_String_Guid(string input)
        {
            // arrange
            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(string), typeof(Guid),
                input, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<Guid>(output);
            Assert.Equal(Guid.Parse(input), output);
        }

        [Fact]
        public void Convert_Guid_String()
        {
            // arrange
            const string expectedOutput = "2d84dcd634394ebe8427f4b1e1730c47";
            Guid input = Guid.Parse(expectedOutput);

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(Guid), typeof(string),
                input, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<string>(output);
            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Convert_String_Uri()
        {
            // arrange
            const string expectedOutput = "http://foo/";

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(string), typeof(Uri),
                expectedOutput, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<Uri>(output);
            Assert.Equal(expectedOutput, output.ToString());
        }

        [Fact]
        public void Convert_Uri_String()
        {
            // arrange
            const string expectedOutput = "http://foo/";
            var input = new Uri(expectedOutput);

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(Uri), typeof(string),
                input, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<string>(output);
            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Convert_String_String()
        {
            // arrange
            const string expectedOutput = "2d84dcd634394ebe8427f4b1e1730c47";

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(string), typeof(string),
                expectedOutput, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<string>(output);
            Assert.Equal(expectedOutput, output);
        }

        [InlineData(1, "1")]
        [InlineData(null, null)]
        [InlineData("foo", "foo")]
        [InlineData(true, "True")]
        [Theory]
        public void Convert_Object_String(object input, object expectedOutput)
        {
            // arrange
            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(object), typeof(string),
                input, out object output);

            // assert
            Assert.True(success);
            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void Convert_ArrayOfString_ListOfString()
        {
            // arrange
            var list = new[] { "a", "b", "c" };

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(string[]), typeof(List<string>),
                list, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<List<string>>(output);
            Assert.Collection((List<string>)output,
                t => Assert.Equal("a", t),
                t => Assert.Equal("b", t),
                t => Assert.Equal("c", t));
        }

        [Fact]
        public void Convert_ArrayOfString_ListOfInt()
        {
            // arrange
            var list = new[] { "1", "2", "3" };

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(string[]), typeof(List<int>),
                list, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<List<int>>(output);
            Assert.Collection((List<int>)output,
                t => Assert.Equal(1, t),
                t => Assert.Equal(2, t),
                t => Assert.Equal(3, t));
        }

        [Fact]
        public void Convert_ArrayOfString_ArraOfInt()
        {
            // arrange
            var list = new[] { "1", "2", "3" };

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(string[]), typeof(int[]),
                list, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<int[]>(output);
            Assert.Collection((int[])output,
                t => Assert.Equal(1, t),
                t => Assert.Equal(2, t),
                t => Assert.Equal(3, t));
        }

        [Fact]
        public void Convert_ArrayOfString_IListOfInt()
        {
            // arrange
            var list = new[] { "1", "2", "3" };

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(string[]), typeof(IList<int>),
                list, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<List<int>>(output);
            Assert.Collection((List<int>)output,
                t => Assert.Equal(1, t),
                t => Assert.Equal(2, t),
                t => Assert.Equal(3, t));
        }

        [Fact]
        public void Convert_ArrayOfString_ICollectionOfInt()
        {
            // arrange
            var list = new[] { "1", "2", "3" };

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(string[]), typeof(ICollection<int>),
                list, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<List<int>>(output);
            Assert.Collection((List<int>)output,
                t => Assert.Equal(1, t),
                t => Assert.Equal(2, t),
                t => Assert.Equal(3, t));
        }

        [Fact]
        public void Convert_ArrayOfString_String()
        {
            // arrange
            var list = new[] { "1", "2", "3" };

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(string[]), typeof(string),
                list, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<string>(output);
            Assert.Equal("1,2,3", output);
        }

        [Fact]
        public void Convert_ArrayOfString_NullableListOfFooOrBar()
        {
            // arrange
            var list = new[] { "Foo", "Bar" };

            // act
            bool success = TypeConversion.Default.TryConvert(
                typeof(string[]), typeof(List<Nullable<FooOrBar>>),
                list, out object output);

            // assert
            Assert.True(success);
            Assert.IsType<List<Nullable<FooOrBar>>>(output);
            Assert.Collection((List<Nullable<FooOrBar>>)output,
                t => Assert.Equal(FooOrBar.Foo, t),
                t => Assert.Equal(FooOrBar.Bar, t));
        }

        public enum FooOrBar
        {
            Foo,
            Bar
        }
    }
}
