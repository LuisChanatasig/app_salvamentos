using app_salvamentos.Models;
using System.Text.RegularExpressions;
using Tesseract;

namespace app_salvamentos.Servicios
{
    public class DocumentRecognitionService
    {
        private readonly string _tessDataPath;
        private readonly string[] _emptyFallback = { "" };

        public DocumentRecognitionService(IWebHostEnvironment env)
        {
            _tessDataPath = Path.Combine(env.ContentRootPath, "tessdata");
        }

        public async Task<VehiculoDatos> RecognizeAsync(Stream imageStream)
        {
            var tmp = Path.GetTempFileName() + ".jpg";
            await using (var fs = new FileStream(tmp, FileMode.Create))
                await imageStream.CopyToAsync(fs);

            try
            {
                using var engine = new TesseractEngine(_tessDataPath, "spa", EngineMode.Default);
                using var img = Pix.LoadFromFile(tmp);
                using var page = engine.Process(img);
                var text = page.GetText() ?? "";

                // Helper único que prueba 3 estrategias de regex:
                string Extract(string label) => FirstMatch(text, new[] { label }, ExtractValue);
                string ExtractAny(params string[] labels) => FirstMatch(text, labels, ExtractValue);
                int ExtractInt(string label) =>
                    int.TryParse(Regex.Match(Extract(label), @"\d+").Value, out var v) ? v : 0;

                var datos = new VehiculoDatos
                {
                    PlacaActual = ExtractAny("PLACA ACTUAL"),
                    PlacaAnterior = ExtractAny("PLACA ANTERIOR"),
                    Ano = ExtractInt("AÑO "),
                    NumeroVin = ExtractAny("NÚMERO VIN", "VIN", "CHASIS"),
                    NumeroMotor = ExtractAny("NÚMERO MOTOR"),
                    RamvCpn = ExtractAny("RAMV/CPN"),
                    Marca = ExtractAny("MARCA"),
                    Modelo = ExtractAny("MODELO"),
                    Cilindraje = ExtractAny("CILINDRAJE"),
                    AnoModelo = ExtractInt("AÑO MODELO"),
                    ClaseVehiculo = ExtractAny("CLASE DE VEHÍCULO", "CLASE DE VEHICULO"),
                    TipoVehiculo = ExtractAny("TIPO DE VEHÍCULO", "TIPO DE VEHICULO"),
                    Pasajeros = ExtractAny("PASAJEROS"),
                    Toneladas = ExtractAny("TONELADAS"),
                    PaisOrigen = ExtractAny("PAÍS DE ORIGEN", "PAIS DE ORIGEN"),
                    Combustible = ExtractAny("COMBUSTIBLE"),
                    Carroceria = ExtractAny("CARROCERÍA", "CARROCERIA"),
                    TipoPeso = ExtractAny("TIPO DE PESO"),
                    Color1 = ExtractAny("COLOR 1"),
                    Color2 = ExtractAny("COLOR 2"),
                    Ortopedico = ExtractAny("ORTOPÉDICO", "ORTOPEDICO"),
                    Remarcado = ExtractAny("REMARCADO"),
                    Observaciones = ExtractAny("OBSERVACIONES")
                };

                // Calidad mínima: al menos 4 campos con texto
                var nonEmpty = datos.GetType()
                                    .GetProperties()
                                    .Select(p => p.GetValue(datos)?.ToString())
                                    .Count(v => !string.IsNullOrWhiteSpace(v));
                if (nonEmpty < 4)
                    throw new Exception("No se detectaron suficientes datos. Revisa calidad/formato.");

                return datos;
            }
            finally
            {
                File.Delete(tmp);
            }
        }

        private string FirstMatch(string text, string[] labels, Func<string, string, string> extractor)
        {
            foreach (var lbl in labels)
            {
                var v = extractor(text, lbl);
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            }
            return "";
        }

        private string ExtractValue(string text, string label)
        {
            // A) LABEL: valor
            var a = Regex.Match(text, $@"(?i)\b{Regex.Escape(label)}\b.*?[:\-–]\s*([^\r\n]+)");
            if (a.Success) return a.Groups[1].Value.Trim();

            // B) LABEL valor
            var b = Regex.Match(text, $@"(?i)\b{Regex.Escape(label)}\b\s+([^\r\n]+)");
            if (b.Success) return b.Groups[1].Value.Trim();

            // C) LABEL\nvalor
            var c = Regex.Match(text, $@"(?i){Regex.Escape(label)}[^\r\n]*[\r\n]+\s*([^\r\n]+)");
            if (c.Success) return c.Groups[1].Value.Trim();

            return "";
        }
    }
}

