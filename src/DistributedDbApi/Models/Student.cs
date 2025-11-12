namespace DistributedDbApi.Models;

public class Student
{
    public string Mssv { get; set; } = null!;
    public string Hoten { get; set; } = null!;
    public string? Phai { get; set; }
    public DateTime? Ngaysinh { get; set; }
    public string Mslop { get; set; } = null!;
    public decimal? Hocbong { get; set; }
}
