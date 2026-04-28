using ExamScheduler.Data;
using ExamScheduler.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamScheduler.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- DERSLER ---
        public async Task<IActionResult> Dersler()
        {
            var dersler = await _context.Dersler.Include(d => d.Bolum).ToListAsync();
            return View(dersler);
        }

        // --- DERSLİKLER (SALONLAR) ---
        public async Task<IActionResult> Derslikler()
        {
            var derslikler = await _context.Derslikler.ToListAsync();
            return View(derslikler);
        }

        // --- PERSONELLER ---
        public async Task<IActionResult> Personeller()
        {
            var personeller = await _context.Personeller.Include(p => p.Bolum).ToListAsync();
            return View(personeller);
        }
    }
}
