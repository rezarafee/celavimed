using Celavimed.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<ClinicService> Services => Set<ClinicService>();
    public DbSet<DoctorService> DoctorServices => Set<DoctorService>();
    public DbSet<DoctorSecretary> DoctorSecretaries => Set<DoctorSecretary>();
    public DbSet<WeeklySchedule> WeeklySchedules => Set<WeeklySchedule>();
    public DbSet<DoctorTimeOff> TimeOffs => Set<DoctorTimeOff>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<WorkflowStage> Stages => Set<WorkflowStage>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentStageLog> StageLogs => Set<AppointmentStageLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<DoctorService>().HasKey(x => new { x.DoctorId, x.ServiceId });
        b.Entity<DoctorService>().HasOne(x => x.Doctor).WithMany(d => d.Services).HasForeignKey(x => x.DoctorId);
        b.Entity<DoctorService>().HasOne(x => x.Service).WithMany(s => s.Doctors).HasForeignKey(x => x.ServiceId);

        b.Entity<DoctorSecretary>().HasKey(x => new { x.DoctorId, x.UserId });
        b.Entity<DoctorSecretary>().HasOne(x => x.Doctor).WithMany(d => d.Secretaries).HasForeignKey(x => x.DoctorId);
        b.Entity<DoctorSecretary>().HasOne(x => x.User).WithMany(u => u.SecretaryOf).HasForeignKey(x => x.UserId);

        b.Entity<WeeklySchedule>().HasIndex(x => new { x.DoctorId, x.DayOfWeek });
        b.Entity<DoctorTimeOff>().HasIndex(x => new { x.DoctorId, x.Date }).IsUnique();

        b.Entity<Patient>().HasIndex(x => x.Mobile).IsUnique();

        b.Entity<Appointment>().HasIndex(x => x.TrackingCode).IsUnique();
        b.Entity<Appointment>().HasIndex(x => new { x.Date, x.StageId });
        // جلوگیری از رزرو هم‌زمان یک نوبت توسط دو نفر
        b.Entity<Appointment>()
            .HasIndex(x => new { x.DoctorId, x.Date, x.StartTime })
            .IsUnique()
            .HasFilter("[Status] = 0");
        b.Entity<Appointment>().HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Appointment>().HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Appointment>().HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Appointment>().HasOne(x => x.Patient).WithMany(p => p.Appointments).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<AppointmentStageLog>().HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AppointmentStageLog>().HasOne(x => x.Appointment).WithMany(a => a.StageLogs).HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
    }
}
