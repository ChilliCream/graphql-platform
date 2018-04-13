using System;
using System.IO;

namespace HotChocolate.Language
{
	/// <summary>
	/// Represents a GraphQL source.
	/// </summary>
	public sealed class Source
		: ISource
		, IEquatable<Source>
	{
		public Source(string body)
		{
			// the normalization might be problematic. 
			// the line count should still work
			Text = body ?? string.Empty;
			Text = Text.Replace("\r\n", "\n")
				.Replace("\n\r", "\n");
		}

		/// <summary>
		/// Gets the GraphQL source text.
		/// </summary>
		/// <returns>
		/// Returns the GraphQL source text.
		/// </returns>
		public string Text { get; }

		/// <summary>
		/// Determines whether the specified <see cref="object"/> is equal 
		/// to the current <see cref="T:HotChocolate.Language.Source"/>.
		/// </summary>
		/// <param name="obj">
		/// The <see cref="object"/> to compare with the current 
		/// <see cref="T:HotChocolate.Language.Source"/>.
		/// </param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="object"/> is equal 
		/// to the current <see cref="T:HotChocolate.Language.Source"/>; 
		/// otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as Source);
		}

		/// <summary>
		/// Determines whether the specified <see cref="Source"/> is equal 
		/// to the current <see cref="T:HotChocolate.Language.Source"/>.
		/// </summary>
		/// <param name="other">
		/// The <see cref="Source"/> to compare with the current 
		/// <see cref="T:HotChocolate.Language.Source"/>.
		/// </param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="Source"/> is equal 
		/// to the current <see cref="T:HotChocolate.Language.Source"/>; 
		/// otherwise, <c>false</c>.
		/// </returns>
		public bool Equals(Source other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Text.Equals(other.Text, StringComparison.Ordinal);
		}

		/// <summary>
		/// Serves as a hash function for a 
		/// <see cref="T:HotChocolate.Language.Source"/> object.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable 
		/// for use in hashing algorithms and data structures such as a
		/// hash table.
		/// </returns>
		public override int GetHashCode()
		{
			return Text.GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents 
		/// the current <see cref="T:HotChocolate.Language.Source"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current 
		/// <see cref="T:HotChocolate.Language.Source"/>.
		/// </returns>
		public override string ToString()
		{
			return Text;
		}
	}
}