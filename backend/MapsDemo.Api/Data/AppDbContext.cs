using Microsoft.EntityFrameworkCore;

namespace MapsDemo.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<GeocodeQuery> GeocodeQueries => Set<GeocodeQuery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GeocodeQuery>(entity =>
        {
            entity.ToTable("geocode_queries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Address)
                .HasColumnName("address")
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(e => e.RequestPayload)
                .HasColumnName("request_payload")
                .HasColumnType("jsonb")
                .IsRequired();
            entity.Property(e => e.ResponsePayload)
                .HasColumnName("response_payload")
                .HasColumnType("jsonb")
                .IsRequired();
            entity.Property(e => e.Latitude)
                .HasColumnName("latitude")
                .HasPrecision(10, 8)
                .IsRequired();
            entity.Property(e => e.Longitude)
                .HasColumnName("longitude")
                .HasPrecision(11, 8)
                .IsRequired();
            entity.Property(e => e.QueriedAt)
                .HasColumnName("queried_at")
                .HasDefaultValueSql("now()")
                .IsRequired();
            entity.HasIndex(e => e.QueriedAt)
                .HasDatabaseName("ix_geocode_queries_queried_at");
        });
    }
}
