using System.ComponentModel.DataAnnotations;

namespace app_salvamentos.Models
{
    /// <summary>
    /// DTO completo para la creación de un Caso, diseñado para recibir datos de un formulario HTML.
    /// </summary>
    public class CrearCasoInputDto
    {
        // Datos del Asegurado
        [Required(ErrorMessage = "El nombre completo del asegurado es obligatorio.")]
        [StringLength(250, ErrorMessage = "El nombre no puede exceder los 250 caracteres.")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El número de cédula del asegurado es obligatorio.")]
        [StringLength(50, ErrorMessage = "La identificación no puede exceder los 50 caracteres.")]
        public string Identificacion { get; set; }

        [Phone(ErrorMessage = "Formato de teléfono inválido.")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder los 20 caracteres.")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
        [StringLength(100, ErrorMessage = "El email no puede exceder los 100 caracteres.")]
        public string? Email { get; set; }

        [StringLength(500, ErrorMessage = "La dirección no puede exceder los 500 caracteres.")]
        public string? Direccion { get; set; }

        // Datos del Vehículo
        [Required(ErrorMessage = "La placa del vehículo es obligatoria.")]
        [StringLength(20, ErrorMessage = "La placa no puede exceder los 20 caracteres.")]
        public string? Placa { get; set; }

        [StringLength(50, ErrorMessage = "La marca no puede exceder los 50 caracteres.")]
        public string? Marca { get; set; }

        [StringLength(50, ErrorMessage = "El modelo no puede exceder los 50 caracteres.")]
        public string? Modelo { get; set; }

        [StringLength(50, ErrorMessage = "La transmisión no puede exceder los 50 caracteres.")]
        public string? Transmision { get; set; }

        [StringLength(50, ErrorMessage = "El combustible no puede exceder los 50 caracteres.")]
        public string? Combustible { get; set; }

        [StringLength(50, ErrorMessage = "El cilindraje no puede exceder los 50 caracteres.")]
        public string? Cilindraje { get; set; }

        [Range(1900, 2100, ErrorMessage = "El año debe estar entre 1900 y 2100.")]
        public int? Anio { get; set; }

        [StringLength(100, ErrorMessage = "El número de chasis no puede exceder los 100 caracteres.")]
        public string? NumeroChasis { get; set; }

        [StringLength(100, ErrorMessage = "El número de motor no puede exceder los 100 caracteres.")]
        public string? NumeroMotor { get; set; }

        [StringLength(50, ErrorMessage = "El tipo de vehículo no puede exceder los 50 caracteres.")]
        public string? TipoVehiculo { get; set; }

        [StringLength(50, ErrorMessage = "La clase no puede exceder los 50 caracteres.")]
        public string? Clase { get; set; }

        [StringLength(50, ErrorMessage = "El color no puede exceder los 50 caracteres.")]
        public string? Color { get; set; }

        [StringLength(4000, ErrorMessage = "Las observaciones no pueden exceder los 4000 caracteres.")]
        public string? ObservacionesVehiculo { get; set; }

        // Datos del Caso
        [StringLength(50, ErrorMessage = "El número de avalúo no puede exceder los 50 caracteres.")]
        public string? NumeroAvaluo { get; set; }

        [StringLength(100, ErrorMessage = "El número de reclamo no puede exceder los 100 caracteres.")]
        public string? NumeroReclamo { get; set; }

        [Required(ErrorMessage = "La fecha del siniestro es obligatoria.")]
        [DataType(DataType.Date, ErrorMessage = "Formato de fecha inválido.")]
        public DateTime FechaSiniestro { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "El estado del caso es obligatorio.")]
        public int CasoEstadoId { get; set; }

        // Listas para los archivos subidos con su metadata
        public List<DocumentoFormInput> DocumentosAsegurado { get; set; } = new List<DocumentoFormInput>();
        public List<DocumentoFormInput> DocumentosCaso { get; set; } = new List<DocumentoFormInput>();
    
    }
}
