using Microsoft.AspNetCore.Mvc.Rendering;

namespace app_salvamentos.Models
{
    public class ModificarCasoViewModel
    {
        public CasoDetalleDto CasoDetalle { get; set; }
        // This new property combines them for easy iteration in the view
        public List<DocumentoFormInput> NewDocuments { get; set; } = new List<DocumentoFormInput>();

        public List<DocumentoDetalleDto> AllDocuments
        {
            get
            {
                var allDocs = new List<DocumentoDetalleDto>();
                if (CasoDetalle?.DocumentosCaso != null)
                {
                    // Ensure ambito_documento is set for each
                    allDocs.AddRange(CasoDetalle.DocumentosCaso.Select(d => { d.ambito_documento = "CASO"; return d; }));
                }
                // Assuming DocumentosAsegurado is also part of CasoDetalle or a separate property in the ViewModel
                // For simplicity, let's assume it's part of CasoDetalle for now, or you pass it separately.
                // If DocumentosAsegurado is directly under Model, you'd do:
                // if (DocumentosAsegurado != null)
                // {
                //     allDocs.AddRange(DocumentosAsegurado.Select(d => { d.ambito_documento = "ASEGURADO"; return d; }));
                // }
                // Let's assume for this example, that Model.CasoDetalle also contains DocumentosAsegurado.
                if (CasoDetalle?.DocumentosAsegurado != null) // Make sure this property exists in CasoDetalleDto
                {
                    allDocs.AddRange(CasoDetalle.DocumentosAsegurado.Select(d => { d.ambito_documento = "ASEGURADO"; return d; }));
                }
                return allDocs;
            }
        }
        public List<SelectListItem> EstadosCaso { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TiposDocumentoCaso { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TiposDocumentoAsegurado { get; set; } = new List<SelectListItem>();
    }
}
