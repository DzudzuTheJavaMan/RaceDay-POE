using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("User", t => t.HasCheckConstraint("CK_User_Role", "[Role] IN ('Organiser', 'Participant')"));
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        b.Entity<Profile>(e =>
        {
            e.ToTable("Profile");
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.Province).HasMaxLength(100);
            e.Property(x => x.ProfilePictureUrl).HasMaxLength(255);
            e.HasOne(x => x.User).WithOne(u => u.Profile)
                .HasForeignKey<Profile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Event>(e =>
        {
            e.ToTable("Event", t => t.HasCheckConstraint("CK_Event_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')"));
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.EventDate).HasColumnType("datetime");
            e.Property(x => x.Location).HasMaxLength(150).IsRequired();
            e.Property(x => x.Distance).HasColumnType("decimal(6,2)");
            e.Property(x => x.EventType).HasMaxLength(20).IsRequired();
            e.Property(x => x.BannerImageUrl).HasMaxLength(255);
            e.HasOne(x => x.Organiser).WithMany(u => u.Events)
                .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Category");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolment", t => t.HasCheckConstraint("CK_Enrolment_Status", "[Status] IN ('Pending', 'Confirmed')"));
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Pending");
            e.HasIndex(x => new { x.ParticipantId, x.EventId }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Event).WithMany(ev => ev.Enrolments)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Result>(e =>
        {
            e.ToTable("Result");
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}