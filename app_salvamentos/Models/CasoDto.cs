namespace app_salvamentos.Models
{
    public class CasoDto
    {  // Asegurado
        public string? NombreCompleto { get; set; }
        public string? Identificacion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }

        // Vehículo
        public string? Placa { get; set; } = null!;
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? Transmision { get; set; }
        public string? Combustible { get; set; }
        public string? Cilindraje { get; set; }
        public int? Anio { get; set; }
        public string? NumeroAvaluo { get; set; }
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

        //nuevos campos
        public string ? MetodoAvaluo { get; set; }
        public string? DireccionAvaluo { get; set; }
        public DateTime? FechaSolicitudAvaluo { get; set; }
        public string? ComentariosAvaluo { get; set; }
        public string? NotasAvaluo { get; set; }

        // Documentos
        public List<DocumentoDto> DocumentosAsegurado { get; set; } = new();
        public List<DocumentoDto> DocumentosCaso { get; set; } = new();

        public int UsuarioId { get; set; }
    }
}
