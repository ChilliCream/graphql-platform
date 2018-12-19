using Xunit;

namespace HotChocolate.Utilities
{
    public class CharExtensionTests
    {
        [Fact]
        public static void IsLetter_LowerCaseLetter()
        {
            for (char c = 'a'; c <= 'z'; c++)
            {
                Assert.True(c.IsLetter());
            }
        }

        [Fact]
        public static void IsLetter_UpperCaseLetter()
        {
            for (char c = 'A'; c <= 'Z'; c++)
            {
                Assert.True(c.IsLetter());
            }
        }


        [Fact]
        public static void IsLetter_AllChars()
        {
            // arrange
            bool[] isLetter = new bool[(int)char.MaxValue + 1];

            for (char c = 'a'; c <= 'z'; c++)
            {
                isLetter[c] = true;
            }

            for (char c = 'A'; c <= 'Z'; c++)
            {
                isLetter[c] = true;
            }

            // check max char
            // act
            bool result = char.MaxValue.IsLetter();

            // check all the other chars
            // assert
            Assert.Equal(isLetter[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsLetter();

                // assert
                Assert.Equal(isLetter[c], result);
            }
        }

        [Fact]
        public static void IsLetterOrUnderscore_LowerCaseLetter()
        {
            for (char c = 'a'; c <= 'z'; c++)
            {
                Assert.True(c.IsLetterOrUnderscore());
            }

            Assert.True('_'.IsLetterOrUnderscore());
        }

        [Fact]
        public static void IsLetterOrUnderscore_AllChars()
        {
            // arrange
            bool[] isLetterOrUnderscore = new bool[(int)char.MaxValue + 1];

            for (char c = 'a'; c <= 'z'; c++)
            {
                isLetterOrUnderscore[c] = true;
            }

            for (char c = 'A'; c <= 'Z'; c++)
            {
                isLetterOrUnderscore[c] = true;
            }

            isLetterOrUnderscore['_'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsLetterOrUnderscore();

            // check all the other chars
            // assert
            Assert.Equal(isLetterOrUnderscore[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsLetterOrUnderscore();

                // assert
                Assert.Equal(isLetterOrUnderscore[c], result);
            }
        }

        [Fact]
        public static void IsDigit()
        {
            for (char c = '0'; c <= '9'; c++)
            {
                Assert.True(c.IsDigit());
            }
        }

        [Fact]
        public static void IsDigit_AllChars()
        {
            // arrange
            bool[] isDigit = new bool[(int)char.MaxValue + 1];

            for (char c = '0'; c <= '9'; c++)
            {
                isDigit[c] = true;
            }

            // check max char
            // act
            bool result = char.MaxValue.IsDigit();

            // check all the other chars
            // assert
            Assert.Equal(isDigit[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsDigit();

                // assert
                Assert.Equal(isDigit[c], result);
            }
        }

        [Fact]
        public static void IsDot()
        {
            Assert.True('.'.IsDot());
        }

        [Fact]
        public static void IsDot_AllChars()
        {
            // arrange
            bool[] isDot = new bool[(int)char.MaxValue + 1];
            isDot['.'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsDot();

            // check all the other chars
            // assert
            Assert.Equal(isDot[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsDot();

                // assert
                Assert.Equal(isDot[c], result);
            }
        }

        [Fact]
        public static void IsHyphen()
        {
            Assert.True('-'.IsHyphen());
        }

        [Fact]
        public static void IsHyphen_AllChars()
        {
            // arrange
            bool[] isHyphen = new bool[(int)char.MaxValue + 1];
            isHyphen['-'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsHyphen();

            // check all the other chars
            // assert
            Assert.Equal(isHyphen[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsHyphen();

                // assert
                Assert.Equal(isHyphen[c], result);
            }
        }

        [Fact]
        public static void IsUnderscore_LowerCaseLetter()
        {
            Assert.True('_'.IsUnderscore());
        }

        [Fact]
        public static void IsUnderscore_AllChars()
        {
            // arrange
            bool[] IsUnderscore = new bool[(int)char.MaxValue + 1];
            IsUnderscore['_'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsUnderscore();

            // check all the other chars
            // assert
            Assert.Equal(IsUnderscore[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsUnderscore();

                // assert
                Assert.Equal(IsUnderscore[c], result);
            }
        }

        [Fact]
        public static void IsMinus()
        {
            Assert.True('-'.IsMinus());
        }

        [Fact]
        public static void IsMinus_AllChars()
        {
            // arrange
            bool[] isMinus = new bool[(int)char.MaxValue + 1];
            isMinus['-'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsMinus();

            // check all the other chars
            // assert
            Assert.Equal(isMinus[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsMinus();

                // assert
                Assert.Equal(isMinus[c], result);
            }
        }

        [Fact]
        public static void IsPlus()
        {
            Assert.True('+'.IsPlus());
        }

        [Fact]
        public static void IsPlus_AllChars()
        {
            // arrange
            bool[] isDigit = new bool[(int)char.MaxValue + 1];
            isDigit['+'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsPlus();

            // check all the other chars
            // assert
            Assert.Equal(isDigit[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsPlus();

                // assert
                Assert.Equal(isDigit[c], result);
            }
        }

        [Fact]
        public static void IsQuote()
        {
            Assert.True('"'.IsQuote());
        }

        [Fact]
        public static void IsQuote_AllChars()
        {
            // arrange
            bool[] isQuote = new bool[(int)char.MaxValue + 1];
            isQuote['"'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsQuote();

            // check all the other chars
            // assert
            Assert.Equal(isQuote[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsQuote();

                // assert
                Assert.Equal(isQuote[c], result);
            }
        }

        [Fact]
        public static void IsBackslash()
        {
            Assert.True('\\'.IsBackslash());
        }

        [Fact]
        public static void IsBackslash_AllChars()
        {
            // arrange
            bool[] isBackslash = new bool[(int)char.MaxValue + 1];
            isBackslash['\\'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsBackslash();

            // check all the other chars
            // assert
            Assert.Equal(isBackslash[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsBackslash();

                // assert
                Assert.Equal(isBackslash[c], result);
            }
        }

        [Fact]
        public static void IsHash()
        {
            Assert.True('#'.IsHash());
        }

        [Fact]
        public static void IsHash_AllChars()
        {
            // arrange
            bool[] isHash = new bool[(int)char.MaxValue + 1];
            isHash['#'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsHash();

            // check all the other chars
            // assert
            Assert.Equal(isHash[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsHash();

                // assert
                Assert.Equal(isHash[c], result);
            }
        }

        [InlineData('!')]
        [InlineData('$')]
        [InlineData('&')]
        [InlineData('(')]
        [InlineData(')')]
        [InlineData(':')]
        [InlineData('=')]
        [InlineData('@')]
        [InlineData('[')]
        [InlineData(']')]
        [InlineData('{')]
        [InlineData('|')]
        [InlineData('}')]
        [InlineData('.')]
        [Theory]
        public static void IsPunctuator(char c)
        {
            Assert.True(c.IsPunctuator());
        }

        [Fact]
        public static void IsPunctuator_AllChars()
        {
            // arrange
            bool[] isPunctuator = new bool[(int)char.MaxValue + 1];
            isPunctuator['!'] = true;
            isPunctuator['$'] = true;
            isPunctuator['&'] = true;
            isPunctuator['('] = true;
            isPunctuator[')'] = true;
            isPunctuator[':'] = true;
            isPunctuator['='] = true;
            isPunctuator['@'] = true;
            isPunctuator['['] = true;
            isPunctuator[']'] = true;
            isPunctuator['{'] = true;
            isPunctuator['|'] = true;
            isPunctuator['}'] = true;
            isPunctuator['.'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsPunctuator();

            // check all the other chars
            // assert
            Assert.Equal(isPunctuator[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsPunctuator();

                // assert
                Assert.Equal(isPunctuator[c], result);
            }
        }

        [InlineData('\t')]
        [InlineData('\r')]
        [InlineData('\n')]
        [InlineData(' ')]
        [InlineData(',')]
        [InlineData((char)0xfeff)]
        [Theory]
        public static void IsWhitespace(char c)
        {
            Assert.True(c.IsWhitespace());
        }

        [Fact]
        public static void IsWhitespace_AllChars()
        {
            // arrange
            bool[] isWhitespace = new bool[(int)char.MaxValue + 1];
            isWhitespace['\t'] = true;
            isWhitespace['\r'] = true;
            isWhitespace['\n'] = true;
            isWhitespace[' '] = true;
            isWhitespace[','] = true;
            isWhitespace[0xfeff] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsWhitespace();

            // check all the other chars
            // assert
            Assert.Equal(isWhitespace[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsWhitespace();

                // assert
                Assert.Equal(isWhitespace[c], result);
            }
        }

        [Fact]
        public static void IsNewLine()
        {
            Assert.True('\n'.IsNewLine());
        }

        [Fact]
        public static void IsNewLine_AllChars()
        {
            // arrange
            bool[] isNewLine = new bool[(int)char.MaxValue + 1];
            isNewLine['\n'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsNewLine();

            // check all the other chars
            // assert
            Assert.Equal(isNewLine[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsNewLine();

                // assert
                Assert.Equal(isNewLine[c], result);
            }
        }

        [Fact]
        public static void IsReturn()
        {
            Assert.True('\r'.IsReturn());
        }

        [Fact]
        public static void IsReturn_AllChars()
        {
            // arrange
            bool[] isReturn = new bool[(int)char.MaxValue + 1];
            isReturn['\r'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsReturn();

            // check all the other chars
            // assert
            Assert.Equal(isReturn[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsReturn();

                // assert
                Assert.Equal(isReturn[c], result);
            }
        }

        [InlineData('"')]
        [InlineData('/')]
        [InlineData('\\')]
        [InlineData('b')]
        [InlineData('f')]
        [InlineData('n')]
        [InlineData('r')]
        [InlineData('t')]
        [Theory]
        public static void IsValidEscapeCharacter(char c)
        {
            Assert.True(c.IsValidEscapeCharacter());
        }

        [InlineData('"', '"')]
        [InlineData('/', '/')]
        [InlineData('\\', '\\')]
        [InlineData('b', '\b')]
        [InlineData('f', '\f')]
        [InlineData('n', '\n')]
        [InlineData('r', '\r')]
        [InlineData('t', '\t')]
        [Theory]
        public static void EscapeCharacter(char input, char output)
        {
            Assert.Equal(output, input.EscapeCharacter());
        }

        [Fact]
        public static void IsValidEscapeCharacter_AllChars()
        {
            // arrange
            bool[] isValidEscapeCharacter = new bool[(int)char.MaxValue + 1];
            isValidEscapeCharacter['"'] = true;
            isValidEscapeCharacter['/'] = true;
            isValidEscapeCharacter['\\'] = true;
            isValidEscapeCharacter['b'] = true;
            isValidEscapeCharacter['f'] = true;
            isValidEscapeCharacter['n'] = true;
            isValidEscapeCharacter['r'] = true;
            isValidEscapeCharacter['t'] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsValidEscapeCharacter();

            // check all the other chars
            // assert
            Assert.Equal(isValidEscapeCharacter[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsValidEscapeCharacter();

                // assert
                Assert.Equal(isValidEscapeCharacter[c], result);
            }
        }

        [InlineData((char)0)]
        [InlineData((char)1)]
        [InlineData((char)2)]
        [InlineData((char)3)]
        [InlineData((char)4)]
        [InlineData((char)5)]
        [InlineData((char)6)]
        [InlineData((char)7)]
        [InlineData((char)8)]
        [InlineData((char)10)]
        [InlineData((char)11)]
        [InlineData((char)12)]
        [InlineData((char)13)]
        [InlineData((char)14)]
        [InlineData((char)15)]
        [InlineData((char)16)]
        [InlineData((char)17)]
        [InlineData((char)18)]
        [InlineData((char)19)]
        [InlineData((char)20)]
        [InlineData((char)21)]
        [InlineData((char)22)]
        [InlineData((char)23)]
        [InlineData((char)24)]
        [InlineData((char)25)]
        [InlineData((char)26)]
        [InlineData((char)27)]
        [InlineData((char)28)]
        [InlineData((char)29)]
        [InlineData((char)30)]
        [InlineData((char)31)]
        [InlineData((char)127)]
        [Theory]
        public static void IsControlCharacter(char c)
        {
            Assert.True(c.IsControlCharacter());
        }

        [Fact]
        public static void IsControlCharacter_AllChars()
        {
            // arrange
            bool[] isControlCharacter = new bool[(int)char.MaxValue + 1];
            for (int i = 0; i < 9; i++)
            {
                isControlCharacter[i] = true;
            }
            for (int i = 10; i <= 31; i++)
            {
                isControlCharacter[i] = true;
            }
            isControlCharacter[127] = true;

            // check max char
            // act
            bool result = char.MaxValue.IsControlCharacter();

            // check all the other chars
            // assert
            Assert.Equal(isControlCharacter[char.MaxValue], result);

            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                // act
                result = c.IsControlCharacter();

                // assert
                Assert.Equal(isControlCharacter[c], result);
            }
        }
    }
}
