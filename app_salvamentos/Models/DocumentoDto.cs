namespace app_salvamentos.Models
{
    public class DocumentoDto
    {
        public int TipoDocumentoId { get; set; }
        public string NombreArchivo { get; set; } = null!;
        public byte[] ContenidoBinario { get; set; } = Array.Empty<byte>();
        public string? Observaciones { get; set; }
    }
}
