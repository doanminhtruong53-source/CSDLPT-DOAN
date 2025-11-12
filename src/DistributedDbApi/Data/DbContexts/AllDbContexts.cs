using Microsoft.EntityFrameworkCore;
using DistributedDbApi.Models;

namespace DistributedDbApi.Data.DbContexts;

// Site 1: Lop K1 (port 5439)
public class LopK1DbContext : DbContext
{
    public LopK1DbContext(DbContextOptions<LopK1DbContext> options) : base(options) { }
    
    public DbSet<Class> LopK1 { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("lop_k1");
            entity.HasKey(e => e.Mslop);
            entity.Property(e => e.Mslop).HasColumnName("mslop");
            entity.Property(e => e.Tenlop).HasColumnName("tenlop");
            entity.Property(e => e.Khoa).HasColumnName("khoa");
        });
    }
}

// Site 2: Lop K2 (port 5433)
public class LopK2DbContext : DbContext
{
    public LopK2DbContext(DbContextOptions<LopK2DbContext> options) : base(options) { }
    
    public DbSet<Class> LopK2 { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("lop_k2");
            entity.HasKey(e => e.Mslop);
            entity.Property(e => e.Mslop).HasColumnName("mslop");
            entity.Property(e => e.Tenlop).HasColumnName("tenlop");
            entity.Property(e => e.Khoa).HasColumnName("khoa");
        });
    }
}

// Site 3: SinhVien K1 (port 5434)
public class SinhVienK1DbContext : DbContext
{
    public SinhVienK1DbContext(DbContextOptions<SinhVienK1DbContext> options) : base(options) { }
    
    public DbSet<Student> SinhVienK1 { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("sinhvien_k1");
            entity.HasKey(e => e.Mssv);
            entity.Property(e => e.Mssv).HasColumnName("mssv");
            entity.Property(e => e.Hoten).HasColumnName("hoten");
            entity.Property(e => e.Phai).HasColumnName("phai");
            entity.Property(e => e.Ngaysinh).HasColumnName("ngaysinh");
            entity.Property(e => e.Mslop).HasColumnName("mslop");
            entity.Property(e => e.Hocbong).HasColumnName("hocbong");
        });
    }
}

// Site 4: SinhVien K2 (port 5435)
public class SinhVienK2DbContext : DbContext
{
    public SinhVienK2DbContext(DbContextOptions<SinhVienK2DbContext> options) : base(options) { }
    
    public DbSet<Student> SinhVienK2 { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("sinhvien_k2");
            entity.HasKey(e => e.Mssv);
            entity.Property(e => e.Mssv).HasColumnName("mssv");
            entity.Property(e => e.Hoten).HasColumnName("hoten");
            entity.Property(e => e.Phai).HasColumnName("phai");
            entity.Property(e => e.Ngaysinh).HasColumnName("ngaysinh");
            entity.Property(e => e.Mslop).HasColumnName("mslop");
            entity.Property(e => e.Hocbong).HasColumnName("hocbong");
        });
    }
}

// Site 5: DangKy Diem1 (port 5436)
public class DangKyDiem1DbContext : DbContext
{
    public DangKyDiem1DbContext(DbContextOptions<DangKyDiem1DbContext> options) : base(options) { }
    
    public DbSet<RegistrationDiem1> DangKyDiem1 { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistrationDiem1>(entity =>
        {
            entity.ToTable("dangky_diem1");
            entity.HasKey(e => new { e.Mssv, e.Msmon });
            entity.Property(e => e.Mssv).HasColumnName("mssv");
            entity.Property(e => e.Msmon).HasColumnName("msmon");
            entity.Property(e => e.Diem1).HasColumnName("diem1");
        });
    }
}

// Site 6: DangKy Diem23 K1 (port 5437)
public class DangKyDiem23K1DbContext : DbContext
{
    public DangKyDiem23K1DbContext(DbContextOptions<DangKyDiem23K1DbContext> options) : base(options) { }
    
    public DbSet<RegistrationDiem23> DangKyDiem23K1 { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistrationDiem23>(entity =>
        {
            entity.ToTable("dangky_diem23_k1");
            entity.HasKey(e => new { e.Mssv, e.Msmon });
            entity.Property(e => e.Mssv).HasColumnName("mssv");
            entity.Property(e => e.Msmon).HasColumnName("msmon");
            entity.Property(e => e.Diem2).HasColumnName("diem2");
            entity.Property(e => e.Diem3).HasColumnName("diem3");
        });
    }
}

// Site 7: DangKy Diem23 K2 (port 5438)
public class DangKyDiem23K2DbContext : DbContext
{
    public DangKyDiem23K2DbContext(DbContextOptions<DangKyDiem23K2DbContext> options) : base(options) { }
    
    public DbSet<RegistrationDiem23> DangKyDiem23K2 { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistrationDiem23>(entity =>
        {
            entity.ToTable("dangky_diem23_k2");
            entity.HasKey(e => new { e.Mssv, e.Msmon });
            entity.Property(e => e.Mssv).HasColumnName("mssv");
            entity.Property(e => e.Msmon).HasColumnName("msmon");
            entity.Property(e => e.Diem2).HasColumnName("diem2");
            entity.Property(e => e.Diem3).HasColumnName("diem3");
        });
    }
}
