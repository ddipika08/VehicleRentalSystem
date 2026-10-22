using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleRentalSystem.Data;
using VehicleRentalSystem.Models;

namespace VehicleRentalSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Payment/Pay?bookingId=1
        [HttpGet]
        public async Task<IActionResult> Pay(int bookingId)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            var booking = await _context.Bookings
                .Include(b => b.Vehicle)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    b.CustomerId == customerId.Value);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.BookingStatus == "Cancelled")
            {
                TempData["Error"] =
                    "Cancelled bookings cannot be paid.";

                return RedirectToAction("MyBookings", "Booking");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == bookingId);

            if (payment == null)
            {
                payment = new Payment
                {
                    BookingId = booking.BookingId,
                    PaymentMethod = string.Empty,
                    Amount = booking.TotalAmount,
                    PaymentStatus = "Pending",
                    PaymentDate = DateTime.UtcNow,
                    Booking = booking
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
            }

            if (payment.PaymentStatus == "Paid")
            {
                TempData["Success"] = "This booking is already paid.";
                return RedirectToAction("Confirmation",
                    new { paymentId = payment.PaymentId });
            }

            await LoadQrDetails(payment);

            return View(payment);
        }

        // POST: Payment/SelectMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectMethod(
            int paymentId,
            string paymentMethod)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p =>
                    p.PaymentId == paymentId &&
                    p.Booking != null &&
                    p.Booking.CustomerId == customerId.Value);

            if (payment == null)
            {
                return NotFound();
            }

            if (payment.PaymentStatus == "Paid")
            {
                TempData["Success"] = "This payment is already completed.";

                return RedirectToAction(
                    nameof(Confirmation),
                    new { paymentId });
            }

            if (payment.Booking!.BookingStatus == "Cancelled")
            {
                TempData["Error"] =
                    "Cancelled bookings cannot be paid.";

                return RedirectToAction("MyBookings", "Booking");
            }

            if (paymentMethod != "Online QR" &&
                paymentMethod != "Cash")
            {
                TempData["Error"] = "Please select a valid payment method.";
                return RedirectToAction(nameof(Pay),
                    new { bookingId = payment.BookingId });
            }

            payment.PaymentMethod = paymentMethod;

            // Payment is not marked Paid until verified.
            payment.PaymentStatus = "Pending";
            payment.TransactionId = null;

            await _context.SaveChangesAsync();

            if (paymentMethod == "Cash")
            {
                return RedirectToAction(
                    nameof(Confirmation),
                    new { paymentId });
            }

            return RedirectToAction(
                nameof(Pay),
                new { bookingId = payment.BookingId });
        }

        // POST: Payment/SubmitTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTransaction(
            int paymentId,
            string transactionId)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p =>
                    p.PaymentId == paymentId &&
                    p.Booking != null &&
                    p.Booking.CustomerId == customerId.Value);

            if (payment == null)
            {
                return NotFound();
            }

            if (payment.PaymentStatus == "Paid")
            {
                TempData["Success"] = "This payment is already completed.";

                return RedirectToAction(
                    nameof(Confirmation),
                    new { paymentId });
            }

            if (payment.Booking!.BookingStatus == "Cancelled")
            {
                TempData["Error"] =
                    "Cancelled bookings cannot be paid.";

                return RedirectToAction("MyBookings", "Booking");
            }

            if (payment.PaymentMethod != "Online QR")
            {
                TempData["Error"] =
                    "Select Online QR payment first.";

                return RedirectToAction(nameof(Pay),
                    new { bookingId = payment.BookingId });
            }

            if (string.IsNullOrWhiteSpace(transactionId) ||
                transactionId.Trim().Length > 100)
            {
                TempData["Error"] =
                    "Enter a valid transaction reference.";

                return RedirectToAction(nameof(Pay),
                    new { bookingId = payment.BookingId });
            }

            payment.TransactionId = transactionId.Trim();
            payment.PaymentStatus = "Pending";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Transaction reference submitted. Payment is awaiting verification.";

            return RedirectToAction(
                nameof(Confirmation),
                new { paymentId });
        }

        // GET: Payment/Confirmation?paymentId=1
        [HttpGet]
        public async Task<IActionResult> Confirmation(int paymentId)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login", "Customer");
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Vehicle)
                .FirstOrDefaultAsync(p =>
                    p.PaymentId == paymentId &&
                    p.Booking != null &&
                    p.Booking.CustomerId == customerId.Value);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        private Task LoadQrDetails(Payment payment)
        {
            var upiId =
                _configuration["UPI:Id"] ?? "your-upi-id@bank";

            var payeeName =
                _configuration["UPI:PayeeName"] ?? "Vehicle Rental";

            ViewBag.UpiId = upiId;
            ViewBag.IsPlaceholderUpi =
                upiId.Contains("your-upi-id", StringComparison.OrdinalIgnoreCase);

            var transactionNote =
                $"VehicleRentalBooking{payment.BookingId}";

            var upiUri =
                $"upi://pay?pa={Uri.EscapeDataString(upiId)}" +
                $"&pn={Uri.EscapeDataString(payeeName)}" +
                $"&am={payment.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&cu=INR" +
                $"&tn={Uri.EscapeDataString(transactionNote)}";

            ViewBag.QrImageUrl =
                "https://api.qrserver.com/v1/create-qr-code/" +
                "?size=260x260&data=" +
                Uri.EscapeDataString(upiUri);

            return Task.CompletedTask;
        }
    }
}