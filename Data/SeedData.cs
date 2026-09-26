using Microsoft.AspNetCore.Identity;
using bici_bussiness.Data;
using bici_bussiness.Models;

namespace bici_bussiness.Data;

public static class SeedData
{
    public static async Task EnsureSeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await ctx.Database.EnsureCreatedAsync();

        if (!ctx.Incidencias.Any())
        {
            ctx.Incidencias.AddRange(
                new Incidencia { Estacion = "Miraflores 1", Descripcion = "Freno delantero averiado", Prioridad = "Alta", Estado = "Abierta" },
                new Incidencia { Estacion = "Miraflores 2", Descripcion = "Cadena suelta", Prioridad = "Media", Estado = "Abierta" },
                new Incidencia { Estacion = "San Isidro 1", Descripcion = "Pinchazo rueda trasera", Prioridad = "Alta", Estado = "Abierta" },
                new Incidencia { Estacion = "San Isidro 2", Descripcion = "Asiento roto", Prioridad = "Baja", Estado = "Abierta" },
                new Incidencia { Estacion = "Barranco 1", Descripcion = "Cambio de marchas atascado", Prioridad = "Media", Estado = "Abierta" },
                new Incidencia { Estacion = "Surco 1", Descripcion = "Luz delantera no enciende", Prioridad = "Baja", Estado = "Abierta" },
                new Incidencia { Estacion = "La Molina 1", Descripcion = "Timbre roto", Prioridad = "Baja", Estado = "Abierta" },
                new Incidencia { Estacion = "Callao 1", Descripcion = "Cuadro fisurado (cerrada ejemplo)", Prioridad = "Alta", Estado = "Cerrada" }
            );
            await ctx.SaveChangesAsync();
        }

        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        const string email = "supervisor@test.com";
        const string pass = "Supervisor123!";
        if (!await roleMgr.RoleExistsAsync("Supervisor"))
            await roleMgr.CreateAsync(new IdentityRole("Supervisor"));
        var user = await userMgr.FindByEmailAsync(email);
        if (user == null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            await userMgr.CreateAsync(user, pass);
        }
        if (!await userMgr.IsInRoleAsync(user, "Supervisor"))
            await userMgr.AddToRoleAsync(user, "Supervisor");
    }
}
