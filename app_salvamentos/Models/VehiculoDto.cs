namespace app_salvamentos.Models
{
    public class VehiculoDto
    {
        public string Placa { get; set; } = string.Empty;
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? Transmision { get; set; }
        public string? Combustible { get; set; }
        public string? Cilindraje { get; set; }
        public int Anio { get; set; }
        public string? NumeroChasis { get; set; }
        public string? NumeroMotor { get; set; }
        public string? TipoVehiculo { get; set; }
        public string? Clase { get; set; }
        public string? Color { get; set; }
        public string? Observaciones { get; set; }
        public string? Gravamen { get; set; }
        public string? PlacasMetalicas { get; set; }
        public string? RadioVehiculo { get; set; }
        public string? EstadoVehiculo { get; set; }
    }
}
