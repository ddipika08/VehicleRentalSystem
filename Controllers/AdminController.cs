
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleRentalSystem.Data;

namespace VehicleRentalSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // =========================
        // ADMIN LOGIN - GET
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("IsAdmin") == "true")
                return RedirectToAction(nameof(Dashboard));

            return View();
        }

        // =========================
        // ADMIN LOGIN - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            var adminEmail = _configuration["Admin:Email"];
            var adminPassword = _configuration["Admin:Password"];

            if (!string.IsNullOrWhiteSpace(email) &&
                !string.IsNullOrWhiteSpace(password) &&
                email == adminEmail &&
                password == adminPassword)
            {
                HttpContext.Session.Clear();
                HttpContext.Session.SetString("IsAdmin", "true");
                HttpContext.Session.SetString(
                    "AdminName", "Administrator");

                return RedirectToAction(nameof(Dashboard));
            }

            ViewBag.Error = "Invalid admin email or password.";
            return View();
        }

        // =========================
        // ADMIN DASHBOARD
        // =========================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin())
                return RedirectToAction(nameof(Login));

            ViewBag.VehicleCount =
                await _context.Vehicles.CountAsync();

            ViewBag.CustomerCount =
                await _context.Customers.CountAsync();

            ViewBag.BookingCount =
                await _context.Bookings.CountAsync();

            ViewBag.PaymentCount =
                await _context.Payments.CountAsync();

            ViewBag.PendingPaymentCount =
                await _context.Payments.CountAsync(
                    p => p.PaymentStatus == "Pending");

            ViewBag.OnRentCount =
                await _context.Bookings.CountAsync(
                    b => b.BookingStatus == "On Rent");

            return View();
        }

        // =========================
        // MANAGE CUSTOMERS
        // =========================
        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            if (!IsAdmin())
                return RedirectToAction(nameof(Login));

            var customers = await _context.Customers
                .OrderByDescending(c => c.CustomerId)
                .ToListAsync();

            return View(customers);
        }

        // =========================
        // MANAGE BOOKINGS
        // =========================
        [HttpGet]
        public async Task<IActionResult> Bookings()
        {
            if (!IsAdmin())
                return RedirectToAction(nameof(Login));

            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .Include(b => b.VehicleReturn)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // =========================
        // PAYMENT MANAGEMENT
        // =========================
        [HttpGet]
        public async Task<IActionResult> Payments()
        {
            if (!IsAdmin())
                return RedirectToAction(nameof(Login));

            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Customer)
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Vehicle)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        // =========================
        // UPDATE PAYMENT STATUS
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(
            int paymentId,
            string paymentStatus)
        {
            if (!IsAdmin())
                return RedirectToAction(nameof(Login));

            var allowedStatuses = new[]
            {
                "Pending",
                "Paid",
                "Failed"
            };

            if (string.IsNullOrWhiteSpace(paymentStatus) ||
                !allowedStatuses.Contains(paymentStatus))
            {
                TempData["Error"] = "Invalid payment status.";
                return RedirectToAction(nameof(Payments));
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction(nameof(Payments));
            }

            // A refunded payment must be handled through refund management.
            if (payment.RefundStatus == "Refunded")
            {
                TempData["Error"] =
                    "This payment has already been refunded.";

                return RedirectToAction(nameof(Payments));
            }

            // Do not mark a refund as paid or failed through this action.
            payment.PaymentStatus = paymentStatus;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment status updated successfully.";

            return RedirectToAction(nameof(Payments));
        }

        // =========================
        // UPDATE REFUND STATUS
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRefundStatus(
            int paymentId,
            string refundStatus,
            decimal refundAmount)
        {
            if (!IsAdmin())
                return RedirectToAction(nameof(Login));

            var allowedStatuses = new[]
            {
                "Pending",
                "Approved",
                "Refunded",
                "Rejected"
            };

            if (string.IsNullOrWhiteSpace(refundStatus) ||
                !allowedStatuses.Contains(refundStatus))
            {
                TempData["Error"] = "Invalid refund status.";
                return RedirectToAction(nameof(Payments));
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction(nameof(Payments));
            }

            if (payment.Booking == null ||
                payment.Booking.BookingStatus != "Cancelled")
            {
                TempData["Error"] =
                    "Refund can only be managed for cancelled bookings.";

                return RedirectToAction(nameof(Payments));
            }

            if (payment.PaymentStatus != "Paid" &&
                payment.PaymentStatus != "Refunded")
            {
                TempData["Error"] =
                    "Only verified payments can be refunded.";

                return RedirectToAction(nameof(Payments));
            }

            if (refundStatus == "Refunded")
            {
                if (refundAmount <= 0 ||
                    refundAmount > payment.Amount)
                {
                    TempData["Error"] =
                        "Refund amount must be greater than zero and cannot exceed the payment amount.";

                    return RedirectToAction(nameof(Payments));
                }

                // Admin should select Refunded only after the actual
                // refund has been processed outside this application.
                payment.RefundAmount = refundAmount;
                payment.RefundStatus = "Refunded";
                payment.RefundDate = DateTime.UtcNow;
                payment.PaymentStatus = "Refunded";
            }
            else
            {
                if (refundStatus == "Approved" &&
                    (refundAmount <= 0 ||
                     refundAmount > payment.Amount))
                {
                    TempData["Error"] =
                        "Enter a valid refund amount before approving.";

                    return RedirectToAction(nameof(Payments));
                }

                payment.RefundStatus = refundStatus;

                if (refundStatus == "Approved" ||
                    refundStatus == "Pending")
                {
                    payment.RefundAmount = refundAmount;
                }

                if (refundStatus == "Rejected")
                {
                    payment.RefundAmount = 0;
                    payment.RefundDate = null;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Refund information updated successfully.";

            return RedirectToAction(nameof(Payments));
        }

        // =========================
        // ADMIN LOGOUT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // =========================
        // ADMIN SESSION CHECK
        // =========================
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("IsAdmin") == "true";
        }
    }
}
