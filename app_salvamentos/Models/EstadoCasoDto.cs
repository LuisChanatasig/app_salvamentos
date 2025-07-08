namespace app_salvamentos.Models
{
    /// <summary>
    /// DTO para representar un estado de caso.
    /// Coincide con las columnas devueltas por sp_ListarEstadosCaso.
    /// </summary>
    public class EstadoCasoDto
    {
        public int estado_id { get; set; }
        public string nombre_estado { get; set; }
        public string descripcion { get; set; }
        public bool activo { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public int created_by { get; set; }
        public int updated_by { get; set; }
    }
}
