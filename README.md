# Plataforma de Incidencias — BiciOps (Examen Parcial)

ASP.NET Core MVC + Identity + EF Core (SQLite). Operaciones registra averías por estación
(estación, descripción, prioridad, estado Abierta/Cerrada) en `/Operaciones/Incidencias`.

- **URL producción:** <pegar URL de Render>
- **Commit desplegado en Render:** <SHA final de main tras fusionar PR C>
- **Cuenta demo:** `supervisor@test.com` / `Supervisor123!`

## Ramas y PRs (orden A → B → C, merges sin squash ni force push)

| Rama | Cambio | PR |
|---|---|---|
| `feature/busqueda-algolia` | Búsqueda servidor con Algolia, solo abiertas; título `…encontradas` | PR #1 |
| `feature/cache-redis` | Caché Redis 60s del listado + invalidación al cerrar; título `…con consulta rápida` | PR #2 |
| `feature/websocket-piehost` | Evento `IncidenciaActualizada` vía PieSocket + actualización sin recarga; título `…en tiempo real` | PR #3 |

Las tres nacen del commit inicial `a166695`. Conflictos resueltos con merge local
(`Merge main into B…`, `Merge main into C…`), visibles con:

```bash
git log --graph --oneline --all
```

## Servicios (solo variables de entorno, sin claves en el repo)

| Variable | Uso |
|---|---|
| `ALGOLIA_APP_ID` / `ALGOLIA_SEARCH_KEY` / `ALGOLIA_INDEX` | Búsqueda servidor (`Services/AlgoliaService.cs`); índice con `objectID` = Id |
| `REDIS_CONNECTION` | `host:puerto,password=…,abortConnect=false` (StackExchange.Redis, clave `incidencias:abiertas`, 60s) |
| `PIESOCKET_CLUSTER` / `PIESOCKET_API_KEY` / `PIESOCKET_API_SECRET` / `PIESOCKET_CHANNEL` | Publish servidor + WS navegador (`Services/PieSocketPublisher.cs`); el Secret jamás sale del servidor |

## Despliegue en Render (vía GitHub)

1. New → Web Service → este repo, rama `main` (o Blueprint con `render.yaml`, runtime Docker).
2. Build/Start los define el `Dockerfile`; la app escucha `$PORT` y SQLite se crea por seed al arrancar
   (disco efímero: los cierres de prueba se pierden al redeplegar; el seed restaura 7 abiertas).
3. Cargar las 8 variables en Environment y desplegar el commit final de `main`.

## Pruebas (producción, usuario supervisor)

1. **Algolia:** buscar `freno` muestra 1; cerrar una incidencia y re-buscar ya no la muestra; `q` vacío lista habitual.
2. **Redis:** 1.ª carga `MISS DB` (pill gris), 2.ª `HIT Redis` (pill verde); al cerrar, siguiente carga `MISS DB`.
3. **PieSocket:** con dos sesiones abiertas, cerrar en una elimina la fila en la otra sin recargar
   (pill `Tiempo real: conectado`); al reconectar, `GET /Operaciones/Estado` reconcilia.
