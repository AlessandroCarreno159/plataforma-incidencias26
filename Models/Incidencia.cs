namespace bici_bussiness.Models;

public class Incidencia
{
    public int Id { get; set; }
    public string Estacion { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Prioridad { get; set; } = "Media"; // Alta/Media/Baja
    public string Estado { get; set; } = "Abierta"; // Abierta/Cerrada
    public DateTime FechaReporte { get; set; } = DateTime.UtcNow;
}
