using Microsoft.EntityFrameworkCore;

namespace BaitBuster.Api.Persistence;

public sealed class BaitBusterDbContext(DbContextOptions<BaitBusterDbContext> options) : DbContext(options)
{
    public DbSet<AnalysisRecord> Analyses => Set<AnalysisRecord>();
    public DbSet<FindingRecord> Findings => Set<FindingRecord>();
}
