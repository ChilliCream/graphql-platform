namespace HotChocolate.Fusion.Rewriters;

/// <summary>
/// Represents an error that occurs during the rewriting process.
/// </summary>
public sealed class RewriterException(string message) : Exception(message);
