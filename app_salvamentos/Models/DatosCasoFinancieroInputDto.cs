using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace app_salvamentos.Models
{
    // Este es el NUEVO DTO que se usa SOLO para recibir la solicitud HTTP en el controlador
    // debido a que necesitas propiedades IFormFile para los documentos.
    public class DatosCasoFinancieroInputDto // <--- Este es el "nuevo" o "de entrada HTTP"
    {
        public int CasoId { get; set; }
        public int UsuarioId { get; set; }
        public int AseguradoId { get; set; }
        public int VehiculoId { get; set; }
        public string NumeroReclamo { get; set; }
        public string? NombreCompleto { get; set; }
        public string? MetodoAvaluo { get; set; }
        public string? DireccionAvaluo { get; set; }
        public string? ComentariosAvaluo { get; set; }
        public string? NotasAvaluo { get; set; }
        public DateTime FechaSiniestro { get; set; }
        public DateTime? FechaSolicitudAvaluo { get; set; }




        [ModelBinder(BinderType = typeof(JsonModelBinder))]
        public VehiculoDto Vehiculo { get; set; }

        [ModelBinder(BinderType = typeof(JsonModelBinder))]
        public List<ValorComercialDto> ValoresComerciales { get; set; }

        [ModelBinder(BinderType = typeof(JsonModelBinder))]
        public List<DanoDto> Danos { get; set; }

        [ModelBinder(BinderType = typeof(JsonModelBinder))]
        public List<ParteDto> Partes { get; set; }

        [ModelBinder(BinderType = typeof(JsonModelBinder))]
        public ResumenFinancieroDto? Resumen { get; set; }

        // ¡Aquí es donde usamos DocumentoInputDto para la carga de archivos!
        public List<DocumentoFormInput> DocumentosCasoInput { get; set; } = new List<DocumentoFormInput>();
        public List<DocumentoFormInput> DocumentosAseguradoInput { get; set; } = new List<DocumentoFormInput>();
        public List<DocumentoFormInput> DocumentosValorComercialInput { get; set; } = new List<DocumentoFormInput>();
        public List<DocumentoFormInput> DocumentosDanoInput { get; set; } = new List<DocumentoFormInput>();



    }

    public class JsonModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
            if (string.IsNullOrEmpty(value))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            try
            {
                var result = JsonConvert.DeserializeObject(value, bindingContext.ModelType);
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            catch (JsonException ex)
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "JSON inválido");
            }

            return Task.CompletedTask;
        }
    }


}
