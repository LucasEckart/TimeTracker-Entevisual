using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker_Entevisual.Models;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Roles (exactos)
        string[] roles = { "Admin", "Moderador", "Usuario" };

        foreach (var r in roles)
        {
            if (!await roleManager.RoleExistsAsync(r))
                await roleManager.CreateAsync(new IdentityRole(r));
        }

        // Admin inicial (desde appsettings.Development.json)
        var adminEmail = config["SeedAdmin:Email"];
        var adminPassword = config["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            throw new Exception("Falta configurar SeedAdmin:Email y/o SeedAdmin:Password en appsettings.Development.json");

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new Usuario
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,

                Nombre = "Admin",
                Apellido = "Sistema",

                // el admin NO necesita el flujo de primer login
                DebeCambiarPassword = false
            };

            var createRes = await userManager.CreateAsync(admin, adminPassword);
            if (!createRes.Succeeded)
            {
                var errors = string.Join(" | ", createRes.Errors.Select(e => e.Description));
                throw new Exception($"No se pudo crear admin: {errors}");
            }
        }

        // Asegurar rol Admin
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}
