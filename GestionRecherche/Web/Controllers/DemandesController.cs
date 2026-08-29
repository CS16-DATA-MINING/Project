using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace GestionRecherche.Web;

public class DemandesController : Controller
{
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(DepotViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // TODO: call DepotService to create the demande

        TempData["Succes"] = "Demande créée (scaffold).";

        return RedirectToAction(nameof(MesDemandes));
    }

    [HttpGet]
    public IActionResult MesDemandes()
    {
        // TODO: load user's demandes
        var vm = new MesDemandesViewModel();
        return View(vm);
    }

    [HttpGet]
    public IActionResult Details(int id, CancellationToken cancellationToken)
    {
        // TODO: load demande details
        var vm = new DemandeDetailsViewModel { Id = id };
        return View(vm);
    }
}
