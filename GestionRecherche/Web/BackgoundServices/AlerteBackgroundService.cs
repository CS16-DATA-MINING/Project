using GestionRecherche.Business;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GestionRecherche.Web.BackgroundServices;

public sealed class AlerteBackgroundService : BackgroundService
{
    /*
     * Permet de créer un nouveau scope.
     *
     * Un scope donne une nouvelle instance
     * d'AppDbContext et d'AlerteService.
     */
    private readonly IServiceScopeFactory _scopeFactory;

    /*
     * Contient la configuration venant
     * du fichier appsettings.json.
     */
    private readonly IOptions<AlerteOptions> _options;

    /*
     * Journal de bord du BackgroundService.
     */
    private readonly ILogger<AlerteBackgroundService> _logger;


    public AlerteBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<AlerteOptions> options,
        ILogger<AlerteBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        /*
         * Récupérer la fréquence écrite dans
         * appsettings.json.
         */
        int frequenceHeures =
            _options.Value.FrequenceHeures;

        /*
         * Protection :
         * si la fréquence est 0 ou négative,
         * utiliser 24 heures.
         */
        if (frequenceHeures <= 0)
        {
            _logger.LogWarning(
                "La fréquence {Frequence} est invalide. " +
                "La valeur de 24 heures sera utilisée.",
                frequenceHeures);

            frequenceHeures = 24;
        }

        _logger.LogInformation(
            "Le robot d'alertes est démarré. " +
            "Fréquence={FrequenceHeures} heure(s).",
            frequenceHeures);

        /*
         * Le minuteur produit un signal toutes
         * les X heures.
         */
        using var minuteur = new PeriodicTimer(
            TimeSpan.FromHours(frequenceHeures));

        try
        {
            /*
             * Tant que l'application fonctionne,
             * attendre le prochain signal.
             */
            while (await minuteur.WaitForNextTickAsync(
                stoppingToken))
            {
                _logger.LogInformation(
                    "Déclenchement automatique du robot d'alertes.");

                /*
                 * Créer un scope pour ce passage.
                 *
                 * Ce scope fournit un nouvel AppDbContext
                 * et un nouvel AlerteService.
                 */
                using IServiceScope scope =
                    _scopeFactory.CreateScope();

                IAlerteService alerteService =
                    scope.ServiceProvider
                        .GetRequiredService<IAlerteService>();

                /*
                 * Appeler la même méthode que le bouton Admin.
                 */
                await alerteService.ExecuterPassageAsync(
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            /*
             * L'application est en train de s'arrêter.
             * Ce n'est pas une erreur.
             */
             
            _logger.LogInformation(
                "Arrêt normal du robot d'alertes.");
        }
    }
}