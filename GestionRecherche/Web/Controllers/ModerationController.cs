using Microsoft.AspNetCore.Mvc;

namespace GestionRecherche.Web;

public class ModerationController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var vm = new ModerationIndexViewModel();
        return View(vm);
    }

    [HttpGet]
    public IActionResult Group(int id)
    {
        var vm = new ModerationGroupViewModel { GroupId = id };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Merge(ModerationMergeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Group", new ModerationGroupViewModel { GroupId = model.GroupId });
        }

        // TODO: call ModerationService to perform merge

        TempData["Succes"] = "Fusion effectuée (scaffold).";
        return RedirectToAction(nameof(Index));
    }
}
