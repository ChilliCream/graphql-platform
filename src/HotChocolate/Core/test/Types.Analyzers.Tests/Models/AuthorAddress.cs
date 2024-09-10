namespace HotChocolate.Types;

public sealed record AuthorAddress(int Id, int AuthorId, string AuthorName, string Street, string City);
