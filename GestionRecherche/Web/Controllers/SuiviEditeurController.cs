using Microsoft.AspNetCore.Mvc;

namespace GestionRecherche.Web;

public class SuiviEditeurController : Controller
{
    [HttpGet]
    public IActionResult Declare(int demandeId)
    {
        var vm = new DeclareSubmissionViewModel { DemandeId = demandeId };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Declare(DeclareSubmissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // TODO: call SuiviEditeurService to record submission

        TempData["Succes"] = "Soumission enregistrée (scaffold).";
        return RedirectToAction("Details", "Demandes", new { id = model.DemandeId });
    }

    [HttpGet]
    public IActionResult Historique(int demandeId)
    {
        var vm = new HistoriqueViewModel { DemandeId = demandeId };
        return View(vm);
    }
}
