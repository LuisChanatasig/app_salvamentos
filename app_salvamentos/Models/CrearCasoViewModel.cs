using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace app_salvamentos.Models
{
    /// <summary>
    /// ViewModel para la vista de creación de un caso completo.
    /// </summary>
    public class CrearCasoViewModel
    {
        // Listas para los dropdowns
        public List<SelectListItem> EstadosCaso { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TiposDocumentoAsegurado { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TiposDocumentoCaso { get; set; } = new List<SelectListItem>();

        // Datos del Asegurado
        [Required(ErrorMessage = "El nombre completo del asegurado es obligatorio.")]
        [StringLength(250, ErrorMessage = "El nombre no puede exceder los 250 caracteres.")]
        [Display(Name = "Nombre del Asegurado")]
        public string NombreCompleto { get; set; }

        [StringLength(50, ErrorMessage = "La identificación no puede exceder los 50 caracteres.")]
        [Display(Name = "Identificación")]
        public string Identificacion { get; set; }

        [Phone(ErrorMessage = "Formato de teléfono inválido.")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder los 20 caracteres.")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
        [StringLength(100, ErrorMessage = "El email no puede exceder los 100 caracteres.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(500, ErrorMessage = "La dirección no puede exceder los 500 caracteres.")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; }

        // Datos del Vehículo
        [Required(ErrorMessage = "La placa del vehículo es obligatoria.")]
        [StringLength(20, ErrorMessage = "La placa no puede exceder los 20 caracteres.")]
        [Display(Name = "Placa")]
        public string Placa { get; set; }

        [StringLength(50, ErrorMessage = "La marca no puede exceder los 50 caracteres.")]
        [Display(Name = "Marca")]
        public string Marca { get; set; }

        [StringLength(50, ErrorMessage = "El modelo no puede exceder los 50 caracteres.")]
        [Display(Name = "Modelo")]
        public string Modelo { get; set; }

        [StringLength(50, ErrorMessage = "La transmisión no puede exceder los 50 caracteres.")]
        [Display(Name = "Transmisión")]
        public string Transmision { get; set; }

        [StringLength(50, ErrorMessage = "El combustible no puede exceder los 50 caracteres.")]
        [Display(Name = "Combustible")]
        public string Combustible { get; set; }

        [StringLength(50, ErrorMessage = "El cilindraje no puede exceder los 50 caracteres.")]
        [Display(Name = "Cilindraje")]
        public string Cilindraje { get; set; }

        [Range(1900, 2100, ErrorMessage = "El año debe estar entre 1900 y 2100.")]
        [Display(Name = "Año")]
        public int? Anio { get; set; }

        [StringLength(100, ErrorMessage = "El número de chasis no puede exceder los 100 caracteres.")]
        [Display(Name = "Número de Chasis")]
        public string NumeroChasis { get; set; }

        [StringLength(100, ErrorMessage = "El número de motor no puede exceder los 100 caracteres.")]
        [Display(Name = "Número de Motor")]
        public string NumeroMotor { get; set; }

        [StringLength(50, ErrorMessage = "El tipo de vehículo no puede exceder los 50 caracteres.")]
        [Display(Name = "Tipo de Vehículo")]
        public string TipoVehiculo { get; set; }

        [StringLength(50, ErrorMessage = "La clase no puede exceder los 50 caracteres.")]
        [Display(Name = "Clase")]
        public string Clase { get; set; }

        [StringLength(50, ErrorMessage = "El color no puede exceder los 50 caracteres.")]
        [Display(Name = "Color")]
        public string Color { get; set; }

        [StringLength(4000, ErrorMessage = "Las observaciones no pueden exceder los 4000 caracteres.")]
        [Display(Name = "Observaciones del Vehículo")]
        public string ObservacionesVehiculo { get; set; }

        // Datos del Caso
        [Required(ErrorMessage = "El número de avalúo es obligatorio.")]
        [StringLength(50, ErrorMessage = "El número de avalúo no puede exceder los 50 caracteres.")]
        [Display(Name = "Número de Avalúo")]
        public string NumeroAvaluo { get; set; }

        [StringLength(100, ErrorMessage = "El número de reclamo no puede exceder los 100 caracteres.")]
        [Display(Name = "Número de Reclamo")]
        public string NumeroReclamo { get; set; }

        [Required(ErrorMessage = "La fecha del siniestro es obligatoria.")]
        [DataType(DataType.Date, ErrorMessage = "Formato de fecha inválido.")]
        [Display(Name = "Fecha del Siniestro")]
        public DateTime FechaSiniestro { get; set; } = DateTime.Today; // Valor por defecto

        [Required(ErrorMessage = "El estado del caso es obligatorio.")]
        [Display(Name = "Estado del Caso")]
        public int CasoEstadoId { get; set; }

        // Usuario de Auditoría (se llenará en el controlador, no en el formulario)
        public int UsuarioId { get; set; }
    }
}
