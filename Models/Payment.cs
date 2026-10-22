
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleRentalSystem.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking? Booking { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Column(TypeName = "numeric(10,2)")]
        [Range(typeof(decimal), "0", "99999999.99")]
        public decimal Amount { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(30)]
        public string PaymentStatus { get; set; } = "Pending";

        [StringLength(100)]
        public string? TransactionId { get; set; }

        // Refund Details
        [Column(TypeName = "numeric(10,2)")]
        [Range(typeof(decimal), "0", "99999999.99")]
        public decimal RefundAmount { get; set; } = 0;

        [Required]
        [StringLength(30)]
        public string RefundStatus { get; set; } = "Not Requested";

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? RefundDate { get; set; }
    }
}
