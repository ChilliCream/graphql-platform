using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Types.BatchResolvers;

public sealed class BatchDbContext(DbContextOptions<BatchDbContext> options) : DbContext(options);
