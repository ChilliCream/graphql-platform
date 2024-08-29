namespace HotChocolate.Execution.TestContext;

public sealed record ProductImage(string Name, Func<Stream> OpenStream);
