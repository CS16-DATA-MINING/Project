using GestionRecherche.Business;
using GestionRecherche.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionRecherche.Web;

[Authorize(
    Policy = PolitiquesAutorisation.Administration)]
    public sealed class AdminController : Controller
{
    private readonly IAlerteService _alerteService;

    public AdminController(
        IAlerteService alerteService)
    {
        _alerteService = alerteService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = new RobotAlertesViewModel();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LancerMaintenant(
        CancellationToken cancellationToken)
    {
        ResultatPassageAlerte resultat =
            await _alerteService.ExecuterPassageAsync(
                cancellationToken);

        if (resultat.EstReussi)
        {
            TempData["Succes"] =
                resultat.Message;
        }
        else
        {
            TempData["Erreur"] =
                resultat.Message;
        }

        var model = new RobotAlertesViewModel
        {
            DernierPassage = resultat
        };

        return View(
            "Index",
            model);
    }
}