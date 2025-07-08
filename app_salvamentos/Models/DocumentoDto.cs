namespace app_salvamentos.Models
{
    public class DocumentoDto
    {
        public int TipoDocumentoId { get; set; }
        public string NombreArchivo { get; set; }
        public string RutaFisica { get; set; } // CAMBIO: Ahora es string para la ruta física
        public string Observaciones { get; set; }
    }
}
