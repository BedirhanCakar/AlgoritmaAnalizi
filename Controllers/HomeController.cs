using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamScheduler.Models;
using ExamScheduler.Data;

namespace ExamScheduler.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Takvim()
    {
        var sinavlar = await _context.Sinavlar
            .Include(s => s.Ders)
            .Include(s => s.Oturum)
            .Include(s => s.Salonlar)
                .ThenInclude(ss => ss.Derslik)
            .Include(s => s.Salonlar)
                .ThenInclude(ss => ss.Gozetmenler)
                    .ThenInclude(g => g.Personel)
            .OrderBy(s => s.Oturum.BaslangicSaat)
            .ToListAsync();

        return View(sinavlar);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
