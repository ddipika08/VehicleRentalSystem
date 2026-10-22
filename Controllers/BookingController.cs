using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleRentalSystem.Data;
using VehicleRentalSystem.Models;

namespace VehicleRentalSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Booking/Create?vehicleId=1
        [HttpGet]
        public async Task<IActionResult> Create(int vehicleId)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                TempData["Error"] = "Please login before booking a vehicle.";
                return RedirectToAction("Login", "Account");
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);

            if (vehicle == null)
            {
                return NotFound();
            }

            if (vehicle.Status != "Available")
            {
                TempData["Error"] = "This vehicle is currently not available.";
                return RedirectToAction("Details", "Vehicle",
                    new { id = vehicleId });
            }

            var booking = new Booking
            {
                VehicleId = vehicle.VehicleId,
                Vehicle = vehicle,
                PickupDate = DateTime.Today,
                ReturnDate = DateTime.Today.AddDays(1)
            };

            return View(booking);
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                TempData["Error"] = "Please login before booking a vehicle.";
                return RedirectToAction("Login", "Account");
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleId == booking.VehicleId);

            if (vehicle == null)
            {
                return NotFound();
            }

            ModelState.Remove("Customer");
            ModelState.Remove("Vehicle");

            if (vehicle.Status != "Available")
            {
                TempData["Error"] = "This vehicle is currently not available.";
                return RedirectToAction("Details", "Vehicle",
                    new { id = booking.VehicleId });
            }

            if (booking.PickupDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(booking.PickupDate),
                    "Pickup date cannot be in the past.");
            }

            if (booking.ReturnDate.Date <= booking.PickupDate.Date)
            {
                ModelState.AddModelError(
                    nameof(booking.ReturnDate),
                    "Return date must be after pickup date.");
            }

            if (booking.ReturnDate.Date > booking.PickupDate.Date)
            {
                var overlappingBooking = await _context.Bookings
                    .AnyAsync(b =>
                        b.VehicleId == booking.VehicleId &&
                        b.BookingStatus != "Cancelled" &&
                        booking.PickupDate.Date < b.ReturnDate.Date &&
                        booking.ReturnDate.Date > b.PickupDate.Date);

                if (overlappingBooking)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "This vehicle is already booked for the selected dates.");
                }
            }

            if (!ModelState.IsValid)
            {
                booking.Vehicle = vehicle;
                return View(booking);
            }

            booking.TotalDays =
                (booking.ReturnDate.Date - booking.PickupDate.Date).Days;

            booking.TotalAmount =
                booking.TotalDays * vehicle.PricePerDay;

            booking.CustomerId = customerId.Value;
            booking.BookingStatus = "Confirmed";

            booking.PickupDate = DateTime.SpecifyKind(
                booking.PickupDate.Date,
                DateTimeKind.Utc);

            booking.ReturnDate = DateTime.SpecifyKind(
                booking.ReturnDate.Date,
                DateTimeKind.Utc);

            booking.BookingDate = DateTime.UtcNow;

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Vehicle booked successfully.";

            return RedirectToAction(nameof(MyBookings));
        }

        // GET: Booking/MyBookings
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                TempData["Error"] = "Please login first.";
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _context.Bookings
                .Include(b => b.Vehicle)
                .Where(b => b.CustomerId == customerId.Value)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Booking/Details/1
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                TempData["Error"] = "Please login first.";
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == id &&
                    b.CustomerId == customerId.Value);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Booking/Cancel/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");

            if (customerId == null)
            {
                TempData["Error"] = "Please login first.";
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b =>
                    b.BookingId == id &&
                    b.CustomerId == customerId.Value);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.BookingStatus == "Cancelled")
            {
                TempData["Error"] = "This booking is already cancelled.";
                return RedirectToAction(nameof(MyBookings));
            }

            if (booking.BookingStatus != "Confirmed")
            {
                TempData["Error"] =
                    "Only confirmed bookings can be cancelled.";

                return RedirectToAction(nameof(MyBookings));
            }

            booking.BookingStatus = "Cancelled";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Booking #{booking.BookingId} cancelled successfully.";

            return RedirectToAction(nameof(MyBookings));
        }
    }
}