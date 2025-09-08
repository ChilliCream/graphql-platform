namespace HotChocolate.Data.TestContext;

public sealed record ProductImage(string Name, Func<Stream> OpenStream);
