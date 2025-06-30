using app_salvamentos.Models;
using app_salvamentos.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace app_salvamentos.Controllers
{
    public class DocumentController : Controller
    {
        private readonly DocumentRecognitionService _docSvc;

        public DocumentController(DocumentRecognitionService docSvc)
        {
            _docSvc = docSvc;
        }

        [HttpPost("scan")]
        public async Task<JsonResult> ScanDocument(IFormFile Matricula)
        {
            if (Matricula == null || Matricula.Length == 0)
            {
                // No TempData ni redirect: devolvemos JSON de error
                return Json(new { error = "No se subió ningún archivo." });
            }

            try
            {
                using var stream = Matricula.OpenReadStream();
                var datos = await _docSvc.RecognizeAsync(stream);
                return Json(datos);  // JSON con los campos en caso de éxito
            }
            catch (Exception ex)
            {
                // JSON con el mensaje de error, sin recargar
                return Json(new { error = ex.Message });
            }
        }

    }
}
