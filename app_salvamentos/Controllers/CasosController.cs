using Microsoft.AspNetCore.Mvc;

namespace app_salvamentos.Controllers
{
    public class CasosController : Controller
    {
        public IActionResult CasosRegistrados()
        {
            return View();
        }  
        public IActionResult CrearCasos()
        {
            return View();
        }


    }
}
