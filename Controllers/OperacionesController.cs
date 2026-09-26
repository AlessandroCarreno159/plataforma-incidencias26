using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using bici_bussiness.Data;
using bici_bussiness.Services;

namespace bici_bussiness.Controllers;

[Authorize]
public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAlgoliaService _algolia;
    private readonly ILogger<OperacionesController> _log;

    public OperacionesController(ApplicationDbContext db, IAlgoliaService algolia, ILogger<OperacionesController> log)
    {
        _db = db; _algolia = algolia; _log = log;
    }

    // GET /Operaciones/Incidencias?q=
    public async Task<IActionResult> Incidencias(string? q)
    {
        ViewBag.Q = q ?? "";
        ViewBag.CacheStatus = "DB"; // B (Redis) lo sobrescribe; hueco reservado
        ViewBag.PieChannel = Environment.GetEnvironmentVariable("PIESOCKET_CHANNEL") ?? "incidencias";

        if (string.IsNullOrWhiteSpace(q))
        {
            var lista = await _db.Incidencias
                .Where(i => i.Estado == "Abierta").OrderBy(i => i.Id).ToListAsync();
            return View(lista);
        }

        // Con texto: servidor consulta Algolia y filtra solo abiertas existentes en base
        var ids = await _algolia.SearchIdsAsync(q);
        if (ids.Count == 0) return View(new List<Models.Incidencia>());
        var filtradas = await _db.Incidencias
            .Where(i => ids.Contains(i.Id) && i.Estado == "Abierta")
            .OrderBy(i => i.Id).ToListAsync();
        _log.LogInformation("Búsqueda q={Q}: {N} abiertas", q, filtradas.Count);
        return View(filtradas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var inc = await _db.Incidencias.FindAsync(id);
        if (inc != null && inc.Estado != "Cerrada")
        {
            inc.Estado = "Cerrada";
            await _db.SaveChangesAsync();
            // B: invalidar Redis aquí. C: publicar IncidenciaActualizada aquí.
            _log.LogInformation("Incidencia {Id} cerrada", id);
        }
        return RedirectToAction(nameof(Incidencias));
    }
}
