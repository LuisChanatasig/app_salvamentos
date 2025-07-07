namespace app_salvamentos.Models
{
    /// <summary>
    /// DTO para representar un perfil.
    /// Coincide con las columnas devueltas por sp_ListarPerfiles.
    /// </summary>
    public class PerfilDto
    {
        public int perfil_id { get; set; }
        public string perfil_nombre { get; set; }
        public string perfil_descripcion { get; set; }
        public bool perfil_estado { get; set; }
        public DateTime create_at { get; set; }
        public DateTime update_at { get; set; }
    }

}
