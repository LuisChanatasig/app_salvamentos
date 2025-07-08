namespace app_salvamentos.Models
{
    public class CasoListadoDto
    {
        public string caso_id { get; set; } // Coincide con el SP
        public string numero_avaluo { get; set; } // Coincide con el SP
        public string nombre_asegurado { get; set; } // Coincide con el SP
        public string placa { get; set; } // Coincide con el SP
        public string marca { get; set; } // Coincide con el SP
        public string modelo { get; set; } // Coincide con el SP
        public string estado_caso { get; set; } // Coincide con el SP
        public DateTime fecha_siniestro { get; set; } // Coincide con el SP
        public string cliente { get; set; } // Coincide con el SP
        public string numero_reclamo { get; set; } // Coincide con el SP
        public DateTime created_at { get; set; } // Coincide con el SP
        public string creado_por { get; set; } // Coincide con el SP
    }

}
