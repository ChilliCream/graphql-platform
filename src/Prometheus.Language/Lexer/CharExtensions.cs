namespace Prometheus.Language
{
	public static class CharExtensions
	{
		public static bool IsLetterOrDigit(this char c)
		{
			return c.IsLetter() || c.IsDigit();
		}

		public static bool IsLetter(this char c)
		{
			return (c >= 65 && c <= 90) // A-Z
				|| (c >= 97 && c <= 122); // a-z
		}

		public static bool IsDigit(this char c)
		{
			return c >= 48 && c <= 57;
		}

		public static bool IsDot(this char c)
		{
			return c == '.';
		}

		public static bool IsHyphen(this char c)
		{
			return c == '-';
		}

		public static bool IsUnderscore(this char c)
		{
			return c == '_';
		}

		public static bool IsMinus(this char c)
		{
			return c.IsHyphen();
		}

		public static bool IsPlus(this char c)
		{
			return c == '+';
		}

		public static bool IsQuote(this char c)
		{
			return c == '"';
		}

		public static bool IsBackslash(this char c)
		{
			return c == '\\';
		}

		public static bool IsHash(this char c)
		{
			return c == '#';
		}

		public static bool IsPunctuator(this char c)
		{
			return c == '!' || c == '$' || c == '&'
				|| c == '(' || c == ')' || c == ':'
				|| c == '=' || c == '@' || c == '['
				|| c == ']' || c == '{' || c == '|'
				|| c == '}' || c == '.';
		}

		public static bool IsWhitespace(this char c)
		{
			return c == '\t' || c == '\r' || c == '\n'
				|| c == ' ' || c == ',' || c == 0xfeff;
		}

		public static bool IsNewLine(this char c)
		{
			// 0x000a
			return c == '\n';
		}

		public static bool IsReturn(this char c)
		{
			// 0x000d
			return c == '\r';
		}

		public static bool IsValidEscapeCharacter(this char c)
		{
			return c == '"' || c == '/' || c == '\\' || c == '\b'
				|| c == '\f' || c == '\n' || c == '\r' || c == '\t';
		}

		public static bool IsTab(this char c)
		{
			// 0x0009
			return c == '\t';
		}

		public static bool IsControlCharacter(this char c)
		{
			return c <= 31 || c == 127;
		}
	}
}