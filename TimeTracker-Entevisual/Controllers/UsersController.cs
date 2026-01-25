using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TimeTracker_Entevisual.Models;
using TimeTracker_Entevisual.Models.ViewModels;

namespace TimeTracker_Entevisual.Controllers
{
    [Authorize(Roles = "Admin,Moderador")]
    public class UsersController : Controller
    {
        private readonly UserManager<Usuario> _userManager;

        public UsersController(UserManager<Usuario> userManager)
        {
            _userManager = userManager;
        }

        // LISTADO
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();

            var vm = new List<UsuarioListItemVM>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);

                vm.Add(new UsuarioListItemVM
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email ?? "",
                    EsAdmin = roles.Contains("Admin"),
                    EsModerador = roles.Contains("Moderador"),
                    DebeCambiarPassword = u.DebeCambiarPassword,
                    Activo = !(u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow)
                });
            }

            // admin arriba
            vm = vm.OrderByDescending(x => x.EsAdmin).ThenBy(x => x.Apellido).ThenBy(x => x.Nombre).ToList();

            return View(vm);
        }



        [HttpGet]
        public IActionResult Create()
        {
            var vm = new UsuarioCreateVM
            {
                Rol = "Usuario"
            };

            vm.RolesDisponibles = GetRolesDisponiblesParaCreador(vm.Rol);
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCreateVM vm)
        {
            // ✅ siempre repoblar dropdown para volver a renderizar bien
            vm.RolesDisponibles = GetRolesDisponiblesParaCreador(vm.Rol);

            if (!ModelState.IsValid)
                return View(vm);

            if (!RolPermitidoParaCreador(vm.Rol))
            {
                ModelState.AddModelError("", "No tenés permisos para crear usuarios con ese rol.");
                return View(vm);
            }

            var email = vm.Email.Trim().ToLower();

            var exists = await _userManager.FindByEmailAsync(email);
            if (exists != null)
            {
                ModelState.AddModelError("", "Ya existe un usuario con ese email.");
                return View(vm);
            }

            var tempPassword = GenerarPasswordTemporal();

            var user = new Usuario
            {
                UserName = email,
                Email = email,
                Nombre = vm.Nombre.Trim(),
                Apellido = vm.Apellido.Trim(),
                EmailConfirmed = true,
                DebeCambiarPassword = true,
                LockoutEnabled = true,
                LockoutEnd = null
            };

            var res = await _userManager.CreateAsync(user, tempPassword);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors)
                    ModelState.AddModelError("", e.Description);

                return View(vm);
            }

            // ✅ rol elegido (Admin puede dar Admin/Moderador; Moderador solo Usuario)
            await _userManager.AddToRoleAsync(user, vm.Rol);

            TempData["Credenciales"] = $"Usuario creado: {email} | Password temporal: {tempPassword} | Rol: {vm.Rol}";
            return RedirectToAction(nameof(Index));
        }


        // EDIT (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var activo = !(user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow);

            return View(new UsuarioEditVM
            {
                Id = user.Id,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Email = user.Email ?? "",
                Activo = activo
            });
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UsuarioEditVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            // si cambia email, hay que cuidar uniqueness
            var newEmail = vm.Email.Trim().ToLower();
            if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _userManager.FindByEmailAsync(newEmail);
                if (exists != null && exists.Id != user.Id)
                {
                    ModelState.AddModelError("", "Ya existe otro usuario con ese email.");
                    return View(vm);
                }

                user.Email = newEmail;
                user.UserName = newEmail;
            }

            user.Nombre = vm.Nombre.Trim();
            user.Apellido = vm.Apellido.Trim();

            // Activo/inactivo via LockoutEnd
            if (vm.Activo)
                user.LockoutEnd = null;
            else
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // “inactivo”

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors)
                    ModelState.AddModelError("", e.Description);

                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }

        // RESET PASSWORD → nueva pass random + fuerza cambio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // generar token + reset
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var tempPassword = GenerarPasswordTemporal();

            var res = await _userManager.ResetPasswordAsync(user, token, tempPassword);
            if (!res.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", res.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            user.DebeCambiarPassword = true;
            await _userManager.UpdateAsync(user);

            // ⚠️ por ahora lo mostramos (después mail real)
            TempData["Credenciales"] = $"Reset OK: {user.Email} | Password temporal: {tempPassword}";

            return RedirectToAction(nameof(Index));
        }

        private static string GenerarPasswordTemporal()
        {
            // cumple: mayúscula + minúscula + número + símbolo + largo
            return $"Tmp{Guid.NewGuid():N}".Substring(0, 8) + "!" + Random.Shared.Next(10, 99);
        }



        private List<SelectListItem> GetRolesDisponiblesParaCreador(string? rolActualSeleccionado = "Usuario")
        {
            // Moderador: solo puede crear Usuario
            if (User.IsInRole("Moderador") && !User.IsInRole("Admin"))
            {
                return new List<SelectListItem>
            {
                new SelectListItem { Value = "Usuario", Text = "Usuario", Selected = rolActualSeleccionado == "Usuario" }
            };
                }

            // Admin: puede crear cualquiera
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Usuario", Text = "Usuario", Selected = rolActualSeleccionado == "Usuario" },
                new SelectListItem { Value = "Moderador", Text = "Moderador", Selected = rolActualSeleccionado == "Moderador" },
                new SelectListItem { Value = "Admin", Text = "Admin", Selected = rolActualSeleccionado == "Admin" }
            };
        }

        private bool RolPermitidoParaCreador(string rolSolicitado)
        {
            rolSolicitado = (rolSolicitado ?? "").Trim();

            if (User.IsInRole("Admin"))
                return rolSolicitado is "Usuario" or "Moderador" or "Admin";

            if (User.IsInRole("Moderador"))
                return rolSolicitado == "Usuario";

            return false;
        }

    }
}
