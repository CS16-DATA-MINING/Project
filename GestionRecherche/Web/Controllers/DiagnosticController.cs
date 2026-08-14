using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionRecherche.Web;
[Authorize(Roles = "Admin")]
public class DiagnosticController : Controller
{

     [HttpGet]
    public IActionResult Admin()
    {
        return Content(
            $"Autorisation Admin réussie. Utilisateur : {User.Identity?.Name}");
    }


}
