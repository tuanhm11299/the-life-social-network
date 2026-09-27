using TheLife.Domain.Stories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TheLife.Infrastructure.Persistence.Configurations;

internal sealed class StoryConfiguration : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ImageUrl).HasMaxLength(500).IsRequired();

        builder.HasOne(s => s.Author)
            .WithMany(u => u.Stories)
            .HasForeignKey(s => s.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Active stories of some authors.
        builder.HasIndex(s => new { s.AuthorId, s.ExpiresAt });
        builder.HasIndex(s => s.ExpiresAt);
    }
}

internal sealed class StoryViewConfiguration : IEntityTypeConfiguration<StoryView>
{
    public void Configure(EntityTypeBuilder<StoryView> builder)
    {
        builder.HasKey(v => new { v.StoryId, v.ViewerId });

        builder.HasOne(v => v.Story)
            .WithMany(s => s.Views)
            .HasForeignKey(v => v.StoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Viewer)
            .WithMany()
            .HasForeignKey(v => v.ViewerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
