using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TimeTracker_Entevisual.Data;
using TimeTracker_Entevisual.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//incluir db context
builder.Services.AddDbContext<TimeTrackerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TimeTrackerDbContext")));

//incluir identity
builder.Services.AddIdentityCore<Usuario>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<TimeTrackerDbContext>()
    .AddSignInManager();

//manejo de la cookie de autenticacion. lo ponemos en default
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
}).AddIdentityCookies();

//configurar ruteo cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    options.LoginPath = "/Usuario/Login";
    options.AccessDeniedPath = "/Usuario/AccessDenied";
});




var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();




using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<TimeTrackerDbContext>();

    // Usuario interno fijo (sin login)
    const string emailInterno = "interno@timetracker.local";

    var usuarioInterno = await context.Users.FirstOrDefaultAsync(u => u.Email == emailInterno);

    if (usuarioInterno == null)
    {
        usuarioInterno = new Usuario
        {
            UserName = emailInterno,
            Email = emailInterno,
            EmailConfirmed = true,
            Nombre = "Usuario",
            Apellido = "Interno"
        };

        context.Users.Add(usuarioInterno);
        await context.SaveChangesAsync();
    }
}


app.Run();
