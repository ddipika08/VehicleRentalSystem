using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleRentalSystem.Data;
using VehicleRentalSystem.Models;

namespace VehicleRentalSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == customer.Email);

            if (existingCustomer != null)
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(customer);
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Registration successful. Please login.";

            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.Email == email &&
                    c.Password == password);

            if (customer == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
            HttpContext.Session.SetString("CustomerName", customer.Name);

            return RedirectToAction("Index", "Home");
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
    }
}