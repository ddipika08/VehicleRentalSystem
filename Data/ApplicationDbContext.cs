using Microsoft.EntityFrameworkCore;
using VehicleRentalSystem.Models;

namespace VehicleRentalSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Vehicle> Vehicles { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<VehicleReturn> VehicleReturns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Vehicle)
                .WithMany(v => v.Bookings)
                .HasForeignKey(b => b.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithOne(b => b.Payment)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VehicleReturn>()
                .HasOne(r => r.Booking)
                .WithOne(b => b.VehicleReturn)
                .HasForeignKey<VehicleReturn>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking dates
            modelBuilder.Entity<Booking>()
                .Property(b => b.PickupDate)
                .HasColumnType("date");

            modelBuilder.Entity<Booking>()
                .Property(b => b.ReturnDate)
                .HasColumnType("date");

            // BookingDate uses UTC
            modelBuilder.Entity<Booking>()
                .Property(b => b.BookingDate)
                .HasColumnType("timestamp with time zone");
        }
    }
}