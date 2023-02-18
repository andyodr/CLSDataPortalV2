using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CLS.WebApi.Data.Models;

namespace CLS.WebApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{

	public ApplicationDbContext() : base() {
	}

	public ApplicationDbContext(DbContextOptions options) : base(options) {
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);
		modelBuilder.Entity<AuditTrail>().HasKey(a => a.Id);
		modelBuilder.Entity<AuditTrail>()
			.HasOne<User>()
			.WithMany()
			.HasForeignKey(a => a.UpdatedBy);  // configure a foreign key without navigation
		modelBuilder.Entity<AuditTrail>().Property(a => a.Type).IsUnicode(false).HasMaxLength(20);
		modelBuilder.Entity<AuditTrail>().Property(a => a.Code).IsUnicode(false).HasMaxLength(20);
		modelBuilder.Entity<AuditTrail>().Property(a => a.Description).IsUnicode(false).HasMaxLength(255);
		modelBuilder.Entity<AuditTrail>().Property(a => a.Data).IsUnicode(false);
		modelBuilder.Entity<AuditTrail>().Metadata.SetIsTableExcludedFromMigrations(true);

		modelBuilder.Entity<Calendar>().HasKey(c => c.Id);
		modelBuilder.Entity<Calendar>().Property(t => t.IsProcessed).HasDefaultValue(2);
		modelBuilder.Entity<Calendar>().Property(t => t.LastUpdatedOn).HasDefaultValueSql("GETDATE()");
		modelBuilder.Entity<Calendar>().Metadata.SetIsTableExcludedFromMigrations(true);
		modelBuilder.Entity<CustomerHierarchy>().ToTable("CustomerHierarchy").HasKey(c => c.Id);
		modelBuilder.Entity<CustomerHierarchy>().Metadata.SetIsTableExcludedFromMigrations(true);
		modelBuilder.Entity<ErrorLog>().HasKey(e => e.Id);
		modelBuilder.Entity<ErrorLog>().Metadata.SetIsTableExcludedFromMigrations(true);
		modelBuilder.Entity<Interval>().HasKey(i => i.Id);
		modelBuilder.Entity<Interval>().Metadata.SetIsTableExcludedFromMigrations(true);
		modelBuilder.Entity<MeasureData>().HasKey(m => m.Id);
		modelBuilder.Entity<MeasureData>().Property(t => t.IsProcessed).HasDefaultValue(1);
		modelBuilder.Entity<MeasureData>().Property(t => t.LastUpdatedOn).HasDefaultValueSql("GETDATE()");
		modelBuilder.Entity<MeasureData>().Metadata.SetIsTableExcludedFromMigrations(true);
		modelBuilder.Entity<MeasureDefinition>().HasKey(d => d.Id);
		modelBuilder.Entity<MeasureDefinition>().HasOne(d => d.Unit).WithMany().HasForeignKey(d => d.UnitId).IsRequired(true);
		modelBuilder.Entity<MeasureDefinition>().Property(d => d.IsProcessed).HasDefaultValue(2);
		modelBuilder.Entity<MeasureDefinition>().Property(d => d.LastUpdatedOn).HasDefaultValueSql("GETDATE()");
		modelBuilder.Entity<MeasureDefinition>().Metadata.SetIsTableExcludedFromMigrations(true);
		modelBuilder.Entity<Measure>().HasKey(m => m.Id);
		modelBuilder.Entity<Measure>().Property(t => t.LastUpdatedOn).HasDefaultValueSql("GETDATE()");
		modelBuilder.Entity<Measure>().Metadata.SetIsTableExcludedFromMigrations(true);
		modelBuilder.Entity<MeasureType>().HasKey(m => m.Id);
		modelBuilder.Entity<MeasureType>().Metadata.SetIsTableExcludedFromMigrations(true);
		modelBuilder.Entity<Hierarchy>().HasKey(h => h.Id);
		modelBuilder.Entity<Hierarchy>()
			.HasOne(h => h.Parent)
			.WithMany(h => h.Children)
			.HasForeignKey(h => h.HierarchyParentId)
			.IsRequired(false)
			.OnDelete(DeleteBehavior.Restrict);
		modelBuilder.Entity<Hierarchy>().Property(t => t.IsProcessed).HasDefaultValue(2);
		modelBuilder.Entity<Hierarchy>().Property(t => t.LastUpdatedOn).HasDefaultValueSql("GETDATE()");
		modelBuilder.Entity<Hierarchy>().Metadata.SetIsTableExcludedFromMigrations(true);

		modelBuilder.Entity<HierarchyLevel>().HasKey(h => h.Id);
		modelBuilder.Entity<HierarchyLevel>().Metadata.SetIsTableExcludedFromMigrations(true);
		modelBuilder.Entity<Target>().HasKey(t => t.Id);
		modelBuilder.Entity<Target>().Property(t => t.IsProcessed).HasDefaultValue(2);
		modelBuilder.Entity<Target>().Property(t => t.LastUpdatedOn).HasDefaultValueSql("GETDATE()");
		modelBuilder.Entity<Target>().Metadata.SetIsTableExcludedFromMigrations(true);
		modelBuilder.Entity<User>().HasKey(u => u.Id);
		modelBuilder.Entity<User>().Property(t => t.LastUpdatedOn).HasDefaultValueSql("GETDATE()");
		modelBuilder.Entity<UserCalendarLock>().HasKey(u => u.Id);
		modelBuilder.Entity<UserCalendarLock>().Property(t => t.LastUpdatedOn).HasDefaultValueSql("GETDATE()");
		modelBuilder.Entity<UserHierarchy>().HasKey(u => u.Id);
		modelBuilder.Entity<UserHierarchy>().Property(t => t.LastUpdatedOn).HasDefaultValueSql("GETDATE()");
		modelBuilder.Entity<UserRole>().HasKey(u => u.Id);
		modelBuilder.Entity<Setting>().HasKey(s => s.Id);
		modelBuilder.Entity<Setting>().Property(t => t.LastUpdatedOn).HasDefaultValueSql("GETDATE()");
		modelBuilder.Entity<Setting>().Metadata.SetIsTableExcludedFromMigrations(true);
		modelBuilder.Entity<Unit>().HasKey(u => u.Id);
		modelBuilder.Entity<Unit>().Metadata.SetIsTableExcludedFromMigrations(true);
	}

	public DbSet<AuditTrail> AuditTrail { get; set; }
	public DbSet<Calendar> Calendar { get; set; }
	public DbSet<CustomerHierarchy> CustomerRegion { get; set; }
	public DbSet<ErrorLog> ErrorLog { get; set; }
	public DbSet<Interval> Interval { get; set; }
	public DbSet<MeasureData> MeasureData { get; set; }
	public DbSet<MeasureDefinition> MeasureDefinition { get; set; }
	public DbSet<Measure> Measure { get; set; }
	public DbSet<MeasureType> MeasureType { get; set; }
	public DbSet<Hierarchy> Hierarchy { get; set; }
	public DbSet<HierarchyLevel> HierarchyLevel { get; set; }
	public DbSet<Target> Target { get; set; }
	public DbSet<User> User { get; set; }
	public DbSet<UserCalendarLock> UserCalendarLock { get; set; }
	public DbSet<UserHierarchy> UserHierarchy { get; set; }
	public DbSet<UserRole> UserRole { get; set; }
	public DbSet<Setting> Setting { get; set; }
	public DbSet<Unit> Unit { get; set; }
}
