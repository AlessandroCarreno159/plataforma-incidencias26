using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using bici_bussiness.Data;
using bici_bussiness.Services;

namespace bici_bussiness.Controllers;

[Authorize]
public class OperacionesController : Controller
{
    private const string ListaKey = "incidencias:abiertas";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly ApplicationDbContext _db;
    private readonly IAlgoliaService _algolia;
    private readonly IDistributedCache _cache;
    private readonly ILogger<OperacionesController> _log;

    public OperacionesController(ApplicationDbContext db, IAlgoliaService algolia, IDistributedCache cache, ILogger<OperacionesController> log)
    {
        _db = db; _algolia = algolia; _cache = cache; _log = log;
    }

    // GET /Operaciones/Incidencias?q=
    public async Task<IActionResult> Incidencias(string? q)
    {
        ViewBag.Q = q ?? "";
        ViewBag.PieChannel = Environment.GetEnvironmentVariable("PIESOCKET_CHANNEL") ?? "incidencias";

        if (!string.IsNullOrWhiteSpace(q))
        {
            // Con texto: Algolia directo, SIN usar la caché del listado.
            ViewBag.CacheStatus = "Directo (sin caché)";
            var ids = await _algolia.SearchIdsAsync(q);
            if (ids.Count == 0) return View(new List<Models.Incidencia>());
            var filtradas = await _db.Incidencias
                .Where(i => ids.Contains(i.Id) && i.Estado == "Abierta")
                .OrderBy(i => i.Id).ToListAsync();
            _log.LogInformation("Búsqueda q={Q}: {N} abiertas", q, filtradas.Count);
            return View(filtradas);
        }

        // Sin texto: listado general cacheado 60s en Redis.
        try
        {
            var cached = await _cache.GetStringAsync(ListaKey);
            if (cached != null)
            {
                ViewBag.CacheStatus = "HIT Redis";
                _log.LogInformation("Listado general: HIT Redis");
                return View(JsonSerializer.Deserialize<List<Models.Incidencia>>(cached) ?? new());
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Redis no disponible al leer, usando base");
        }

        var lista = await _db.Incidencias
            .Where(i => i.Estado == "Abierta").OrderBy(i => i.Id).ToListAsync();
        ViewBag.CacheStatus = "MISS DB";
        _log.LogInformation("Listado general: MISS DB ({N} abiertas)", lista.Count);
        try
        {
            await _cache.SetStringAsync(ListaKey, JsonSerializer.Serialize(lista),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Redis no disponible al escribir, se sirve de base");
        }
        return View(lista);
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
            // Invalidar la clave del listado ANTES de volver a consultarlo.
            try
            {
                await _cache.RemoveAsync(ListaKey);
                _log.LogInformation("Redis invalidado ({Key}) tras cerrar {Id}", ListaKey, id);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo invalidar Redis tras cerrar {Id}", id);
            }
            // C: publicar IncidenciaActualizada aquí.
            _log.LogInformation("Incidencia {Id} cerrada", id);
        }
        return RedirectToAction(nameof(Incidencias));
    }
}
