namespace app_salvamentos.Models
{
    /// <summary>
    /// Clase genérica para encapsular resultados paginados.
    /// </summary>
    /// <typeparam name="T">El tipo de los elementos en la lista paginada.</typeparam>
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

}
