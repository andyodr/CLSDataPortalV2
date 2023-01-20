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

		modelBuilder.Entity<Calendar>().HasKey(c => c.Id);
		modelBuilder.Entity<CustomerHierarchy>().ToTable("CustomerHierarchy").HasKey(c => c.Id);
		modelBuilder.Entity<ErrorLog>().HasKey(e => e.Id);
		modelBuilder.Entity<Interval>().HasKey(i => i.Id);
		modelBuilder.Entity<MeasureData>().HasKey(m => m.Id);
		modelBuilder.Entity<MeasureDefinition>().HasKey(m => m.Id);
		modelBuilder.Entity<Measure>().HasKey(m => m.Id);
		modelBuilder.Entity<MeasureType>().HasKey(m => m.Id);
		modelBuilder.Entity<Hierarchy>().HasKey(h => h.Id);
		modelBuilder.Entity<HierarchyLevel>().HasKey(h => h.Id);
		modelBuilder.Entity<Target>().HasKey(t => t.Id);
		modelBuilder.Entity<User>().HasKey(u => u.Id);
		modelBuilder.Entity<UserCalendarLock>().HasKey(u => u.Id);
		modelBuilder.Entity<UserHierarchy>().HasKey(u => u.Id);
		modelBuilder.Entity<UserRole>().HasKey(u => u.Id);
		modelBuilder.Entity<Setting>().HasKey(s => s.Id);
		modelBuilder.Entity<Unit>().HasKey(u => u.Id);

		//modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
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
