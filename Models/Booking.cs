
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleRentalSystem.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public Vehicle? Vehicle { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime PickupDate { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime ReturnDate { get; set; }

        public int TotalDays { get; set; }

        [Column(TypeName = "numeric(10,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(30)]
        public string BookingStatus { get; set; } = "Pending";

        [Column(TypeName = "timestamp with time zone")]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? ActualPickupDate { get; set; }

        public string? PickupConfirmedBy { get; set; }

        public Payment? Payment { get; set; }

        public VehicleReturn? VehicleReturn { get; set; }
    }
}
