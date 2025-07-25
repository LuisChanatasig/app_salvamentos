namespace app_salvamentos.Models
{
    public class DocumentoDetalleDto
    {
        public int documento_id { get; set; }
        public int tipo_documento_id { get; set; }
        
        public string ? ambito_documento { get; set; }
        public string? nombre_tipo { get; set; } // Nuevo campo del SP
        public string? nombre_archivo { get; set; }
        public string? tipo_documento_nombre { get; set; }
        public string? ruta_fisica { get; set; }
        public string? observaciones { get; set; }
        public DateTime fecha_subida { get; set; }

        // Aquí agregas esta propiedad solo para la URL pública
        public string RutaPublica { get; set; }
    }
}
