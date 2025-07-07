using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace app_salvamentos.Models
{
    public class CrearUsuarioViewModel
    {
        // Propiedades para la lista desplegable de perfiles
        public List<SelectListItem> Perfiles { get; set; } = new List<SelectListItem>();

        // Propiedades para enlazar desde el formulario de creación de usuario
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre de usuario no puede exceder los 100 caracteres.")]
        [Display(Name = "Nombre de Usuario")]
        public string UsuarioLogin { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
        [StringLength(255, ErrorMessage = "El correo electrónico no puede exceder los 255 caracteres.")]
        [Display(Name = "Correo Electrónico")]
        public string UsuarioEmail { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 50 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } // Contraseña en texto plano ingresada por el usuario

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
        [Display(Name = "Confirmar Contraseña")]
        public string ConfirmPassword { get; set; } // Campo para confirmar la contraseña

        [Required(ErrorMessage = "Debe seleccionar un perfil.")]
        [Display(Name = "Perfil")]
        public int PerfilId { get; set; } // ID del perfil seleccionado por el usuario

        // Nota: PasswordSalt y UsuarioPasswordHash no se exponen directamente en el ViewModel
        // porque se generan en el backend (servicio) antes de enviar a la base de datos.
    }
}
