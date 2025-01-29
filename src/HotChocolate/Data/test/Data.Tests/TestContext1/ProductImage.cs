namespace HotChocolate.Data.TestContext1;

public sealed record ProductImage(string Name, Func<Stream> OpenStream);
