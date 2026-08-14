using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionRecherche.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{

public AppDbContext (DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }


public DbSet<Uer> Uers => Set<Uer>();

    public DbSet<Laboratoire> Laboratoires => Set<Laboratoire>();

    public DbSet<Journal> Journaux => Set<Journal>();

    public DbSet<DemandePublication> DemandesPublication
        => Set<DemandePublication>();

    public DbSet<JournalCibleDemande> JournauxCiblesDemande
        => Set<JournalCibleDemande>();

    public DbSet<EtapeValidation> EtapesValidation
        => Set<EtapeValidation>();

    public DbSet<Anomalie> Anomalies => Set<Anomalie>();

    public DbSet<HistoriqueStatutEditeur> HistoriquesStatutEditeur
        => Set<HistoriqueStatutEditeur>();

    public DbSet<ParametreSysteme> ParametresSysteme
        => Set<ParametreSysteme>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}
