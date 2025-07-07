using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace app_salvamentos.Models
{
    // ViewModel para el formulario de edición de usuario
    public class EditarUsuarioViewModel
    {
        public List<SelectListItem> Perfiles { get; set; } = new List<SelectListItem>();

        [Required]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre de usuario no puede exceder los 100 caracteres.")]
        [Display(Name = "Nombre de Usuario")]
        public string UsuarioLogin { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
     
        [StringLength(255, ErrorMessage = "El correo electrónico no puede exceder los 255 caracteres.")]
        [Display(Name = "Nickname o  Email")]
        public string UsuarioEmail { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un perfil.")]
        [Display(Name = "Perfil")]
        public int PerfilId { get; set; }

        [Display(Name = "Estado (Activo)")]
        public bool UsuarioEstado { get; set; }
    }
}
