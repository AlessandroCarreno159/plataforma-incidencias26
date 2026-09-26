using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using bici_bussiness.Data;
using Microsoft.EntityFrameworkCore;

namespace bici_bussiness.Services;

// Pregunta 1 (Algolia): el servidor consulta el índice y devuelve IDs.
// El controller filtra solo Abiertas existentes en la base. Sin credenciales hay fallback EF.
public interface IAlgoliaService
{
    Task<List<int>> SearchIdsAsync(string query);
}

public class AlgoliaService : IAlgoliaService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AlgoliaService> _log;
    public AlgoliaService(ApplicationDbContext db, ILogger<AlgoliaService> log)
    {
        _db = db; _log = log;
    }

    public async Task<List<int>> SearchIdsAsync(string query)
    {
        // Solo clave de búsqueda (search-only). La Admin/Write NUNCA se usa aquí ni llega al navegador.
        var appId = Environment.GetEnvironmentVariable("ALGOLIA_APP_ID");
        var searchKey = Environment.GetEnvironmentVariable("ALGOLIA_SEARCH_KEY");
        var indexName = Environment.GetEnvironmentVariable("ALGOLIA_INDEX") ?? "incidencias";

        if (!string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(searchKey))
        {
            try
            {
                var client = new SearchClient(appId, searchKey);
                var response = await client.SearchSingleIndexAsync<Hit>(
                    indexName,
                    new SearchParams(new SearchParamsObject
                    {
                        Query = query,
                        HitsPerPage = 50,
                        AttributesToRetrieve = new List<string> { "objectID" }
                    }));
                var ids = (response.Hits ?? new List<Hit>())
                    .Select(h => int.TryParse(h.ObjectID, out var id) ? id : -1)
                    .Where(id => id > 0).Distinct().ToList();
                _log.LogInformation("Algolia [{Index}] q={Q}: {N} hits", indexName, query, ids.Count);
                return ids;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Algolia falló, fallback EF para q={Q}", query);
            }
        }
        else
        {
            _log.LogInformation("Algolia sin credenciales, fallback EF para q={Q}", query);
        }

        return await _db.Incidencias
            .Where(i => i.Estacion.Contains(query) || i.Descripcion.Contains(query))
            .Select(i => i.Id).ToListAsync();
    }
}
