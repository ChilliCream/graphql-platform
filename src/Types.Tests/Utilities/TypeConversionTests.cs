using System;
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
        public void ConvertNumbers(object input, object expectedOutput,
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

    }
}
