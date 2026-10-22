
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleRentalSystem.Data;
using VehicleRentalSystem.Models;

namespace VehicleRentalSystem.Controllers
{
    public class VehicleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public VehicleController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Customer and Admin: View vehicles
        public async Task<IActionResult> Index()
        {
            var vehicles = await _context.Vehicles
                .OrderByDescending(v => v.VehicleId)
                .ToListAsync();

            return View(vehicles);
        }

        // Customer and Admin: View vehicle details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleId == id);

            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        // ADMIN ONLY: Create GET
        [AdminOnly]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ADMIN ONLY: Create POST
        [AdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Vehicle vehicle,
            IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
                return View(vehicle);

            if (imageFile != null && imageFile.Length > 0)
            {
                string folder = Path.Combine(
                    _environment.WebRootPath,
                    "images"
                );

                Directory.CreateDirectory(folder);

                string extension =
                    Path.GetExtension(imageFile.FileName);

                string fileName =
                    Guid.NewGuid().ToString() + extension;

                string filePath =
                    Path.Combine(folder, fileName);

                using (var stream = new FileStream(
                    filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                vehicle.Image = "/images/" + fileName;
            }

            vehicle.Status = "Available";

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Vehicle added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ADMIN ONLY: Edit GET
        [AdminOnly]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var vehicle = await _context.Vehicles.FindAsync(id);

            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        // ADMIN ONLY: Edit POST
        [AdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Vehicle vehicle,
            IFormFile? imageFile)
        {
            if (id != vehicle.VehicleId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(vehicle);

            var existingVehicle =
                await _context.Vehicles.FindAsync(id);

            if (existingVehicle == null)
                return NotFound();

            existingVehicle.VehicleName = vehicle.VehicleName;
            existingVehicle.Brand = vehicle.Brand;
            existingVehicle.Model = vehicle.Model;
            existingVehicle.VehicleNumber = vehicle.VehicleNumber;
            existingVehicle.Category = vehicle.Category;
            existingVehicle.FuelType = vehicle.FuelType;
            existingVehicle.SeatingCapacity =
                vehicle.SeatingCapacity;
            existingVehicle.PricePerDay =
                vehicle.PricePerDay;
            existingVehicle.Description =
                vehicle.Description;
            existingVehicle.Status =
                vehicle.Status;

            if (imageFile != null && imageFile.Length > 0)
            {
                string folder = Path.Combine(
                    _environment.WebRootPath,
                    "images"
                );

                Directory.CreateDirectory(folder);

                string extension =
                    Path.GetExtension(imageFile.FileName);

                string fileName =
                    Guid.NewGuid().ToString() + extension;

                string filePath =
                    Path.Combine(folder, fileName);

                using (var stream = new FileStream(
                    filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                existingVehicle.Image =
                    "/images/" + fileName;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Vehicle updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ADMIN ONLY: Delete GET
        [AdminOnly]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleId == id);

            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        // ADMIN ONLY: Delete POST
        [AdminOnly]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vehicle =
                await _context.Vehicles.FindAsync(id);

            if (vehicle == null)
                return NotFound();

            // Vehicle with existing bookings cannot be deleted.
            bool hasBookings = await _context.Bookings
                .AnyAsync(b => b.VehicleId == id);

            if (hasBookings)
            {
                TempData["Error"] =
                    "This vehicle has booking records and cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Vehicle deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
