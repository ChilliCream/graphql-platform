using System.ComponentModel.DataAnnotations;

namespace GreenDonut.Data.TestContext;

public abstract class FileSystemEntry
{
    public int Id { get; set; }

    [MaxLength(256)]
    public required string Name { get; set; }

    public int? ParentId { get; set; }

    public FileSystemDirectory? Parent { get; set; }
}

public class FileSystemDirectory : FileSystemEntry
{
    public List<FileSystemEntry> Children { get; set; } = [];
}

public class FileSystemFile : FileSystemEntry
{
    public long Size { get; set; }

    [MaxLength(8)]
    public string? Extension { get; set; }
}
