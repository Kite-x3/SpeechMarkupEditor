using Microsoft.EntityFrameworkCore;
using SpeechMarkupEditor.Infrastructure.Data.Entities;

namespace SpeechMarkupEditor.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<RecognitionModelEntity> RecognitionModels => Set<RecognitionModelEntity>();
    public DbSet<MarkupHistoryEntryEntity> MarkupHistoryEntries => Set<MarkupHistoryEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecognitionModelEntity>(entity =>
        {
            entity.ToTable("RecognitionModels");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.Engine).IsRequired();
            entity.Property(x => x.Path).IsRequired();
            entity.HasIndex(x => x.Path).IsUnique();
        });

        modelBuilder.Entity<MarkupHistoryEntryEntity>(entity =>
        {
            entity.ToTable("MarkupHistoryEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.PayloadJson).IsRequired();
        });
    }
}
