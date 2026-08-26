using GestionRecherche.Business;

using Microsoft.AspNetCore.Mvc;

namespace GestionRecherche.Web;

public sealed class AnomaliesBadgeViewComponent
    : ViewComponent
{
    private readonly IDashboardService _dashboardService;

    public AnomaliesBadgeViewComponent(
        IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!HttpContext.User.IsInRole("ChefBureau"))
        {
            return Content(string.Empty);
        }

        int nombre =
            await _dashboardService
                .CompterAnomaliesOuvertesAsync(
                    HttpContext.RequestAborted);

        return View(nombre);
    }
}