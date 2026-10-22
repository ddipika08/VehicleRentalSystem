
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleRentalSystem.Models
{
    public class VehicleReturn
    {
        public int VehicleReturnId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking? Booking { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime ExpectedReturnDate { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? ActualReturnDate { get; set; }

        public int LateDays { get; set; }

        [Column(TypeName = "numeric(10,2)")]
        public decimal LateCharge { get; set; }

        [StringLength(30)]
        public string ReturnStatus { get; set; } = "Not Returned";

        public int? PickupOdometer { get; set; }

        public int? ReturnOdometer { get; set; }

        [StringLength(500)]
        public string? ConditionAtPickup { get; set; }

        [StringLength(500)]
        public string? ConditionAtReturn { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? HandoverConfirmedAt { get; set; }
    }
}
