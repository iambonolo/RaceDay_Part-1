using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RaceEvent> Events => Set<RaceEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Roles: RoleName must be unique. The two roles are seeded here.
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleName).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.RoleName).IsUnique();
            e.HasData(
                new Role { RoleId = 1, RoleName = RoleNames.Organiser },
                new Role { RoleId = 2, RoleName = RoleNames.Participant });
        });

        // Users: unique email, hashed password, foreign key to Roles.
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.HasOne(x => x.Role)
             .WithMany(r => r.Users)
             .HasForeignKey(x => x.RoleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Events: EventType limited to Run, Walk or Cycle. Foreign key to the Organiser.
        modelBuilder.Entity<RaceEvent>(e =>
        {
            e.ToTable("Events", t => t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run','Walk','Cycle')"));
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            e.Property(x => x.EventDetails).HasMaxLength(1000);
            e.Property(x => x.Venue).HasMaxLength(150).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(8, 2);
            e.Property(x => x.EventType).HasMaxLength(10).IsRequired();
            if (Database.IsSqlServer())
            {
                e.Property(x => x.EventDate).HasColumnType("date");
            }
            e.HasOne(x => x.Organiser)
             .WithMany(u => u.OrganisedEvents)
             .HasForeignKey(x => x.OrganiserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Categories: each belongs to one event.
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Event)
             .WithMany(ev => ev.Categories)
             .HasForeignKey(x => x.EventId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Enrolments: links Participant, Event and Category. One enrolment per participant per event.
        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments");
            e.HasKey(x => x.EnrolmentId);
            e.Property(x => x.EnrolmentStatus).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.ParticipantId, x.EventId }).IsUnique();
            e.HasOne(x => x.Participant)
             .WithMany(u => u.Enrolments)
             .HasForeignKey(x => x.ParticipantId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Event)
             .WithMany(ev => ev.Enrolments)
             .HasForeignKey(x => x.EventId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category)
             .WithMany(c => c.Enrolments)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Results: one result per enrolment (unique foreign key).
        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.ResultId);
            e.HasIndex(x => x.EnrolmentId).IsUnique();
            e.HasOne(x => x.Enrolment)
             .WithOne(en => en.Result)
             .HasForeignKey<Result>(x => x.EnrolmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}