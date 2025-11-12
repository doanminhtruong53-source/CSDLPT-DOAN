namespace DistributedDbApi.Models;

public class RegistrationDiem1
{
    public string Mssv { get; set; } = null!;
    public string Msmon { get; set; } = null!;
    public decimal? Diem1 { get; set; }
}

public class RegistrationDiem23
{
    public string Mssv { get; set; } = null!;
    public string Msmon { get; set; } = null!;
    public decimal? Diem2 { get; set; }
    public decimal? Diem3 { get; set; }
}
