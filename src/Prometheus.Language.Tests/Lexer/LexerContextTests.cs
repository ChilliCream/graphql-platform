using System;
using Xunit;

namespace Prometheus.Language
{
	public class LexerContextTests
	{
		public ISource Source { get; } = new Source("12345");

		[Fact]
		public void Read()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			char c = context.Read();

			// assert
			Assert.Equal(1, context.Position);
			Assert.Equal('1', c);
		}

		[Fact]
		public void ReadRange()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			string s = context.Read(2, 3);

			// assert
			Assert.Equal(0, context.Position);
			Assert.Equal("3", s);
		}

		[Fact]
		public void ReadEmptyRange()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			string s = context.Read(1, 1);

			// assert
			Assert.Equal(0, context.Position);
			Assert.Equal(string.Empty, s);
		}

		[Fact]
		public void IsEndOfStream_BeginOfStream()
		{
			// arrange
			LexerContext context = new LexerContext(new Source("1"));

			// act
			bool endOfStream = context.IsEndOfStream();

			// assert
			Assert.False(endOfStream);
			Assert.Equal(0, context.Position);
		}

		[Fact]
		public void IsEndOfStream_EndOfStream()
		{
			// arrange
			LexerContext context = new LexerContext(new Source("1"));

			// act
			context.Skip();
			bool endOfStream = context.IsEndOfStream();

			// assert
			Assert.True(endOfStream);
			Assert.Equal(1, context.Position);
		}

		[Fact]
		public void Peek()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			char? c = context.Peek();

			// assert
			Assert.Equal(0, context.Position);
			Assert.NotNull(c);
			Assert.Equal('1', c.Value);
		}

		[Fact]
		public void PeekTest()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			bool result = context.PeekTest(c => c == '1');

			// assert
			Assert.True(result);
		}

		[Fact]
		public void PeekTest_With_Two_Valid_Funcs()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			bool result = context.PeekTest(c => c == '1', c => c == '2');

			// assert
			Assert.True(result);
		}

		[Fact]
		public void PeekTest_With_Two_Invalid_Funcs()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			bool result = context.PeekTest(c => c != '1', c => c != '2');

			// assert
			Assert.False(result);
		}

		[Fact]
		public void PeekTest_With_Two_Funcs_EndOfFile()
		{
			// arrange
			LexerContext context = new LexerContext(new Source("1"));

			// act
			bool result = context.PeekTest(c => c != '1', c => c != '2');

			// assert
			Assert.False(result);
		}

		[Fact]
		public void PeekTest_With_Two_Valid_Chars()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			bool result = context.PeekTest('2', '3');

			// assert
			Assert.False(result);
		}

		[Fact]
		public void Skip()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			context.Skip();
			char c = context.Read();

			// assert
			Assert.Equal(2, context.Position);
			Assert.Equal('2', c);
		}

		[Fact]
		public void Skip_With_Count()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			context.Skip(2);
			char c = context.Read();

			// assert
			Assert.Equal(3, context.Position);
			Assert.Equal('3', c);
		}

		[Fact]
		public void ReadPrevious()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			context.Skip(2);
			char c = context.ReadPrevious();

			// assert
			Assert.Equal(2, context.Position);
			Assert.Equal('2', c);
		}

		[Fact]
		public void NewLine()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			context.Skip();
			context.NewLine();

			// assert
			Assert.Equal(1, context.Position);
			Assert.Equal(2, context.Line);
			Assert.Equal(1, context.LineStart);
			Assert.Equal(1, context.Column);
		}

		[Fact]
		public void NewLine_On_Position_2()
		{
			// arrange
			LexerContext context = new LexerContext(Source);

			// act
			context.Skip(2);
			context.NewLine();

			// assert
			Assert.Equal(2, context.Position);
			Assert.Equal(2, context.Line);
			Assert.Equal(2, context.LineStart);
			Assert.Equal(1, context.Column);
		}
	}
}
