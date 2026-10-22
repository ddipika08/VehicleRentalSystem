using System.ComponentModel.DataAnnotations;

namespace VehicleRentalSystem.Models
{
    public class Vehicle
    {
        public int VehicleId { get; set; }

        [Required]
        [StringLength(100)]
        public string VehicleName { get; set; } = string.Empty;

        [Required]
        public string Brand { get; set; } = string.Empty;

        [Required]
        public string Model { get; set; } = string.Empty;

        [Required]
        public string VehicleNumber { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        public string FuelType { get; set; } = string.Empty;

        public int SeatingCapacity { get; set; }

        [Range(1, 100000)]
        public decimal PricePerDay { get; set; }

        public string Description { get; set; } = string.Empty;

        public string? Image { get; set; }

        public string Status { get; set; } = "Available";

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}