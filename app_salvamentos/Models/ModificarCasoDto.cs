namespace app_salvamentos.Models
{
    public class ModificarCasoDto
    {// Id del caso
        public int? CasoId { get; set; } // Permitir nulo para la creación de un nuevo caso

        // Asegurado
        public string NombreCompleto { get; set; } = null!;
        public string? Identificacion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }

        // Vehículo
        public string Placa { get; set; } = null!;
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? Transmision { get; set; }
        public string? Combustible { get; set; }
        public string? Cilindraje { get; set; }
        public int? Anio { get; set; }
        public string? NumeroChasis { get; set; }
        public string? NumeroMotor { get; set; }
        public string? TipoVehiculo { get; set; }
        public string? Clase { get; set; }
        public string? Color { get; set; }
        public string? ObservacionesVehiculo { get; set; }

        // Caso
        public string? NumeroReclamo { get; set; }
        public DateTime FechaSiniestro { get; set; }
        public int CasoEstadoId { get; set; }

        // Documentos
        public List<DocumentoDto> DocumentosAsegurado { get; set; } = new();
        public List<DocumentoDto> DocumentosCaso { get; set; } = new();

        public int UsuarioId { get; set; }
    }
}
