using System.Text;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Expands an event stream topic template into a concrete topic.
/// </summary>
/// <remarks>
/// A template may contain placeholders of the form <c>{$expression}</c> that are resolved
/// to their runtime value. Literal braces are written by doubling them (<c>{{</c> for a literal
/// <c>{</c> and <c>}}</c> for a literal <c>}</c>), mirroring the .NET composite formatting and
/// interpolated string conventions. A single unescaped brace that is neither the start of a
/// placeholder nor part of an escape sequence is an authoring error and is rejected.
/// </remarks>
internal static class TopicTemplate
{
    /// <summary>
    /// Expands the placeholders and brace escapes in the specified <paramref name="topic"/>
    /// template, resolving each <c>{$expression}</c> placeholder with the supplied
    /// <paramref name="resolveExpression"/> delegate.
    /// </summary>
    /// <param name="topic">
    /// The topic template to expand.
    /// </param>
    /// <param name="resolveExpression">
    /// Resolves the value of a placeholder expression (the text between <c>{$</c> and the closing
    /// <c>}</c>).
    /// </param>
    /// <returns>
    /// The expanded topic.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The template contains an unescaped brace or an unterminated placeholder.
    /// </exception>
    public static string Expand(
        string topic,
        TopicExpressionResolver resolveExpression)
    {
        // Fast path: a template without any brace cannot contain a placeholder or an escape, so it
        // is already a concrete topic and is returned verbatim.
        if (topic.IndexOf('{') < 0 && topic.IndexOf('}') < 0)
        {
            return topic;
        }

        var builder = new StringBuilder(topic.Length);
        var i = 0;

        while (i < topic.Length)
        {
            var c = topic[i];

            if (c == '{')
            {
                if (i + 1 < topic.Length && topic[i + 1] == '{')
                {
                    // "{{" is an escaped literal "{".
                    builder.Append('{');
                    i += 2;
                    continue;
                }

                if (i + 1 < topic.Length && topic[i + 1] == '$')
                {
                    // "{$" starts a placeholder. The next single "}" closes it; that "}" is the
                    // delimiter and is never treated as the start of a "}}" escape.
                    var end = topic.IndexOf('}', i + 2);
                    if (end < 0)
                    {
                        throw new InvalidOperationException(
                            $"The topic template `{topic}` contains an unterminated placeholder.");
                    }

                    var expression = topic.AsSpan(i + 2, end - (i + 2));
                    builder.Append(resolveExpression(expression));
                    i = end + 1;
                    continue;
                }

                throw new InvalidOperationException(
                    $"The topic template `{topic}` contains an unescaped `{{`. "
                    + "Use `{{` to write a literal brace.");
            }

            if (c == '}')
            {
                if (i + 1 < topic.Length && topic[i + 1] == '}')
                {
                    // "}}" is an escaped literal "}".
                    builder.Append('}');
                    i += 2;
                    continue;
                }

                throw new InvalidOperationException(
                    $"The topic template `{topic}` contains an unescaped `}}`. "
                    + "Use `}}` to write a literal brace.");
            }

            builder.Append(c);
            i++;
        }

        return builder.ToString();
    }
}

/// <summary>
/// Resolves the value of an event stream topic placeholder expression.
/// </summary>
/// <param name="expression">
/// The placeholder expression (the text between <c>{$</c> and the closing <c>}</c>).
/// </param>
/// <returns>
/// The resolved value to emit in place of the placeholder.
/// </returns>
internal delegate string TopicExpressionResolver(ReadOnlySpan<char> expression);
