using GestionRecherche.Business;
using GestionRecherche.Data;
using GestionRecherche.Web;
using GestionRecherche.Web.BackgroundServices;
using GestionRecherche.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "La chaîne de connexion 'DefaultConnection' est introuvable.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(
        new AutoValidateAntiforgeryTokenAttribute());
});
  
builder.Services.AddScoped<
    IAuthorizationHandler,
    UtilisateurActifHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        PolitiquesAutorisation.ValidationInterne,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireRole(
                RolesMetier.ChefLabo,
                RolesMetier.ChefUer,
                RolesMetier.ChefBureau,
                RolesMetier.SecretariatConseil);

            policy.AddRequirements(
                new UtilisateurActifRequirement());
        });

    options.AddPolicy(
        PolitiquesAutorisation.ChefBureauActif,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(RolesMetier.ChefBureau);

            policy.AddRequirements(
                new UtilisateurActifRequirement());
        });

    options.AddPolicy(
        PolitiquesAutorisation.Administration,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(RolesMetier.Admin);

            policy.AddRequirements(
                new UtilisateurActifRequirement());
        });
});





  builder.Services.AddScoped<IValidationService,ValidationService>();


 builder.Services.AddScoped<IRoutingService, RoutingService>();



 builder.Services.AddScoped<IDashboardService, DashboardService>();
 
 builder.Services.AddScoped<IAnomalieService, AnomalieService>();

  builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);



builder.Services.AddScoped<IAlerteService, AlerteService>();

builder.Services
    .AddOptions<AlerteOptions>()
    .Bind(builder.Configuration.GetSection(AlerteOptions.SectionName));

builder.Services.AddHostedService<AlerteBackgroundService>();



var app = builder.Build();

if (!app.Environment.IsDevelopment())
{

    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}



app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// L'ordre est important.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await IdentitySeeder.SeedAsync(
    app.Services,
    app.Configuration,
    seedDevelopmentUser: app.Environment.IsDevelopment());

//LE Test de jour 5 ( crer deux demeande a et)
if (app.Environment.IsDevelopment())
{
   await GestionRecherche.Web
       .Jour5TestDataSeeder
     .SeedAsync(app.Services);
}

app.Run();