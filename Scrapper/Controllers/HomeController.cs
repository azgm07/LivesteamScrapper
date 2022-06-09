using Scrapper.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Scrapper.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        _logger.LogInformation("Home page loaded");
        return View();
    }

    public IActionResult Privacy()
    {
        _logger.LogInformation("Privacy page loaded");
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
