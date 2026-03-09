namespace HotChocolate.Data.TestContext2;

public sealed record ProductImage(string Name, Func<Stream> OpenStream);
