using Microsoft.AspNetCore.Mvc.Rendering;

namespace app_salvamentos.Models
{
    public class CasoDetalleDto
    {
        public string caso_id { get; set; }
        public string numero_avaluo { get; set; }
        public string numero_reclamo { get; set; }
        public DateTime fecha_siniestro { get; set; }
        public string cliente { get; set; }

        // Datos del asegurado
        public int asegurado_id { get; set; }
        public string nombre_asegurado { get; set; }
        public string identificacion { get; set; }
        public string telefono { get; set; }
        public string email { get; set; }
        public string direccion { get; set; }

        // Datos del vehículo
        public int vehiculo_id { get; set; }
        public string placa { get; set; }
        public string marca { get; set; }
        public string modelo { get; set; }
        public string transmision { get; set; }
        public string combustible { get; set; }
        public string cilindraje { get; set; }
        public int? anio { get; set; } // Nullable, as it can be NULL in DB
        public string numero_chasis { get; set; }
        public string numero_motor { get; set; }
        public string tipo_vehiculo { get; set; }
        public string clase { get; set; }
        public string color { get; set; }
        public string observaciones_vehiculo { get; set; } // Matches the alias in SP

        // Estado del caso
        public int caso_estado_id { get; set; }
        public string estado_caso { get; set; }

        // Metadata
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; } // Nullable, as it can be NULL in DB
        public string creado_por { get; set; }
        public string actualizado_por { get; set; } // Nullable, as it can be NULL in DB

        // Listas para los dropdowns
        public List<SelectListItem> EstadosCaso { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TiposDocumentoAsegurado { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TiposDocumentoCaso { get; set; } = new List<SelectListItem>();
    }

}
