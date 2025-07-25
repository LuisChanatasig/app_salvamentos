using System.ComponentModel.DataAnnotations;

namespace app_salvamentos.Models
{
    /// <summary>
    /// DTO para un documento individual enviado desde el formulario, incluyendo el archivo.
    /// </summary>
    public class DocumentoFormInput
    {
        public int TipoDocumentoId { get; set; }

        public IFormFile? File { get; set; }

        public string? Observaciones { get; set; }

        public string? AmbitoDocumento { get; set; } // <--- ¡Añadir esta propiedad aquí!

    }
}
