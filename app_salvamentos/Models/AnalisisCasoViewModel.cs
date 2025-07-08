using Microsoft.AspNetCore.Mvc.Rendering;

namespace app_salvamentos.Models
{
    /// <summary>
    /// ViewModel para la vista de Análisis de Caso.
    /// Contiene los detalles del caso y la lista de tipos de documento.
    /// </summary>
    public class AnalisisCasoViewModel
    {
        public app_salvamentos.Models.CasoDetalleDto CasoDetalle { get; set; }
        public List<SelectListItem> TiposDocumentoCaso { get; set; } = new List<SelectListItem>();
    }
}
