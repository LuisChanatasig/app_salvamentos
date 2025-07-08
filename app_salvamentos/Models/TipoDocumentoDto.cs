namespace app_salvamentos.Models
{
    public class TipoDocumentoDto
    {
        public int tipo_documento_id { get; set; }
        public string nombre_tipo { get; set; }
        public string ambito_documento { get; set; }
        public string descripcion { get; set; }
        public bool activo { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public int created_by { get; set; }
        public int updated_by { get; set; }
    }
}
