using Microsoft.AspNetCore.Mvc.Rendering;

namespace app_salvamentos.Models
{

    public class CasoDetalleDto
    {
        public int caso_id { get; set; } // Cambiado a int
        public string? numero_avaluo { get; set; }
        public string? numero_reclamo { get; set; }
        public DateTime? fecha_siniestro { get; set; }
        public DateTime? fecha_solicitud_avaluo { get; set; }
        public string? cliente { get; set; }
        public string? metodo_avaluo { get; set; }
        public string? direccion_avaluo { get; set; }
        public string? comentarios_avaluo { get; set; }
        public string? notas_avaluo { get; set; }

        // Datos del asegurado
        public int asegurado_id { get; set; }
        public string? nombre_asegurado { get; set; }
        public string? identificacion { get; set; } // Puede ser nullable si no es PK
        public string? telefono { get; set; }
        public string? email { get; set; }
        public string? direccion { get; set; }

        // Datos del vehículo
        public int vehiculo_id { get; set; }
        public string? radio_vehiculo { get; set; } // Cambiado a string? (en SP es radio_vehiculo)
        public string? placas_metalicas { get; set; } // Cambiado a string?
        public string? gravamen { get; set; } // Cambiado a string? (en SP es gravamen)
        public string? placa { get; set; } // Puede ser nullable si no es PK
        public string? estado_vehiculo { get; set; }
        public string? marca { get; set; }
        public string? modelo { get; set; }
        public string? transmision { get; set; }
        public string? combustible { get; set; }
        public string? cilindraje { get; set; }
        public int? anio { get; set; }
        public string? numero_chasis { get; set; }
        public string? numero_motor { get; set; }
        public string? tipo_vehiculo { get; set; }
        public string? clase { get; set; }
        public string? color { get; set; }
        public string? observaciones_vehiculo { get; set; }

        // Estado del caso
        public int caso_estado_id { get; set; }
        public string? estado_caso { get; set; }

        // Metadatos
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public string? creado_por { get; set; }
        public string? actualizado_por { get; set; }

        // Propiedad para el Resumen Financiero (Segundo Conjunto)
        public ResumenFinancieroDto? Resumen { get; set; }

        // Listas para los detalles relacionados (Tercer, Cuarto y Quinto Conjunto)
        public List<ValorComercialDto> ValoresComerciales { get; set; } = new List<ValorComercialDto>();

        // Propiedades computadas para acceder a los valores por fuente
        public decimal? ValorPatioTuerca => ValoresComerciales
            .FirstOrDefault(vc => vc.Fuente == "Patio Tuerca")?.Valor;

        public decimal? ValorAEADE => ValoresComerciales
            .FirstOrDefault(vc => vc.Fuente == "AEADE")?.Valor;

        public decimal? ValorMarketplace => ValoresComerciales
            .FirstOrDefault(vc => vc.Fuente == "Marketplace")?.Valor;

        public decimal? ValorHugoVargas => ValoresComerciales
            .FirstOrDefault(vc => vc.Fuente == "Hugo Vargas")?.Valor;

        public decimal? ValorOtros => ValoresComerciales
            .FirstOrDefault(vc => vc.Fuente == "Otros")?.Valor;
        public List<DanoDto> Danos { get; set; } = new List<DanoDto>();
        public List<ParteDto> Partes { get; set; } = new List<ParteDto>();

        // Colecciones para los documentos (Sexto al Décimo Conjunto)
        public List<DocumentoDetalleDto> DocumentosAsegurado { get; set; } = new List<DocumentoDetalleDto>();
        public List<DocumentoDetalleDto> DocumentosCaso { get; set; } = new List<DocumentoDetalleDto>();
        public List<DocumentoDetalleDto> DocumentosValorComercial { get; set; } = new List<DocumentoDetalleDto>();
        public List<DocumentoDetalleDto> DocumentosDano { get; set; } = new List<DocumentoDetalleDto>();
        public List<DocumentoDetalleDto> DocumentosPartes { get; set; } = new List<DocumentoDetalleDto>();

        // Listas para los dropdowns (si se cargan aquí)
        public List<SelectListItem> EstadosCaso { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TiposDocumentoAsegurado { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TiposDocumentoCaso { get; set; } = new List<SelectListItem>();
    }

}
