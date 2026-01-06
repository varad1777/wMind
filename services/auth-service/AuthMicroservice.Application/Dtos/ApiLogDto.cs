public class ApiLogDto
{
    public int Id { get; set; }
    public DateTime TimeStamp { get; set; }
    public string Message { get; set; }
    public string UserName { get; set; }
    public string Method { get; set; }
    public string Path { get; set; }
    public int StatusCode { get; set; }
}