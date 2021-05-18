using System;
using System.Linq;
using HotChocolate.Data.Neo4J.Extensions;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J
{
    /// <summary>
    /// Represents methods that can be used to ensure that parameter values meet expected
    /// conditions.
    /// </summary>
    public static class Ensure
    {
        /// <summary>
        /// Ensures that the value of a parameter is not null.
        /// </summary>
        /// <typeparam name="T">Type type of the value.</typeparam>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <returns>The value of the parameter.</returns>
        public static void IsNotNull<T>(T value, string paramName)
        {
            _ = value ?? throw new ArgumentNullException(paramName, "Value cannot be null.");
        }

        /// <summary>
        /// Ensures that the value of a string contains text.
        /// </summary>
        /// <param name="text">The value that is being checked if it contains text.</param>
        /// <param name="message">The message displayed to user if exception is thrown.</param>
        /// <exception cref="ArgumentException">The exception is there is no text.</exception>
        public static void HasText(string text, string message)
        {
            if (!text.HasText())
            {
                throw new ArgumentException(message);
            }
        }

        public static void IsTrue(bool expression, string message)
        {
            if (!expression)
            {
                throw new ArgumentException(message);
            }
        }

        public static void IsNotEmpty(string[] objects, string message)
        {
            if (!objects.Any())
            {
                throw new ArgumentException(message);
            }
        }

        public static void IsNotEmpty(Expression[] objects, string message)
        {
            if (!objects.Any())
            {
                throw new ArgumentException(message);
            }
        }
    }
}
