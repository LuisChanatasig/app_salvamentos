namespace app_salvamentos.Helper
{
    public class DocumentDisplayHelper
    {
        public static string GetMimeTypeFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                // Añade más tipos según necesites
                _ => "application/octet-stream", // Tipo genérico si no se reconoce
            };
        }

        public static string GetIconClass(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType)) return "ri-file-line";

            if (mimeType.Contains("image")) return "ri-image-line";
            if (mimeType.Contains("pdf")) return "ri-file-pdf-line";
            if (mimeType.Contains("word") || mimeType.Contains("msword") || mimeType.Contains("officedocument.wordprocessingml")) return "ri-file-word-line";
            if (mimeType.Contains("excel") || mimeType.Contains("spreadsheetml")) return "ri-file-excel-line";
            // Puedes añadir más lógica aquí para otros tipos (ej. PowerPoint, texto, etc.)
            return "ri-file-line"; // Icono por defecto
        }

        public static string GetColorClass(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType)) return "text-secondary";

            if (mimeType.Contains("image")) return "text-primary";
            if (mimeType.Contains("pdf")) return "text-danger";
            if (mimeType.Contains("word") || mimeType.Contains("msword") || mimeType.Contains("officedocument.wordprocessingml")) return "text-info";
            if (mimeType.Contains("excel") || mimeType.Contains("spreadsheetml")) return "text-success";
            // Puedes añadir más colores para otros tipos
            return "text-secondary"; // Color por defecto
        }
    }
}
