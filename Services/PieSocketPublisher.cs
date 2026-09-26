using System.Text;
using System.Text.Json;

namespace bici_bussiness.Services;

// Pregunta 3 (PieHost/PieSocket): publica eventos desde el SERVIDOR.
// Usa API Secret (variable de entorno). El Secret NUNCA llega al navegador.
public interface IPieSocketPublisher
{
    Task PublishAsync(string evento, object data);
}

public class PieSocketPublisher : IPieSocketPublisher
{
    private readonly HttpClient _http;
    private readonly ILogger<PieSocketPublisher> _log;
    public PieSocketPublisher(HttpClient http, ILogger<PieSocketPublisher> log)
    {
        _http = http; _log = log;
    }

    public async Task PublishAsync(string evento, object data)
    {
        var cluster = Environment.GetEnvironmentVariable("PIESOCKET_CLUSTER") ?? "free.blr2";
        var key = Environment.GetEnvironmentVariable("PIESOCKET_API_KEY");
        var secret = Environment.GetEnvironmentVariable("PIESOCKET_API_SECRET");
        var channel = Environment.GetEnvironmentVariable("PIESOCKET_CHANNEL") ?? "incidencias";
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
        {
            _log.LogWarning("PieSocket sin credenciales, evento {Evento} no publicado", evento);
            return;
        }
        try
        {
            var payload = new { key, secret, channelId = channel, message = new { @event = evento, data } };
            var res = await _http.PostAsync(
                $"https://{cluster}.piesocket.com/api/publish",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            res.EnsureSuccessStatusCode();
            _log.LogInformation("PieSocket publicado {Evento} en {Canal}", evento, channel);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Fallo al publicar {Evento} en PieSocket", evento);
        }
    }
}
