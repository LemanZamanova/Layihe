using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Timoto.Models;


namespace Timoto.DAL
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Car> Cars { get; set; }
        public DbSet<FuelType> FuelTypes { get; set; }
        public DbSet<TransmissionType> TransmissionTypes { get; set; }
        public DbSet<Models.DriveType> DriveTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BodyType> BodyTypes { get; set; }
        public DbSet<VehicleType> VehicleTypes { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<CarFeature> CarFeatures { get; set; }
        public DbSet<CarImage> CarImages { get; set; }
        public DbSet<UserCard> UserCards { get; set; }

        public DbSet<FavoriteCar> FavoriteCars { get; set; }
        public DbSet<Notification> Notifications { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CarFeature>()
                .HasKey(cf => new { cf.CarId, cf.FeatureId });

            modelBuilder.Entity<CarFeature>()
                .HasOne(cf => cf.Car)
                .WithMany(c => c.CarFeatures)
                .HasForeignKey(cf => cf.CarId);

            modelBuilder.Entity<CarFeature>()
                .HasOne(cf => cf.Feature)
                .WithMany(f => f.CarFeatures)
                .HasForeignKey(cf => cf.FeatureId);


            modelBuilder.Entity<Car>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        }


    }
}
