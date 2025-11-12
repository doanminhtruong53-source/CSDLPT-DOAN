// Configure Serilog
using Microsoft.EntityFrameworkCore;
using Serilog;
using DistributedDbApi.Data.DbContexts;
using DistributedDbApi.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // Add Controllers
    builder.Services.AddControllers();

    // Add OpenAPI/Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Distributed Database API",
            Version = "v1",
            Description = "API Gateway cho hệ thống CSDL phân tán quản lý sinh viên (7 PostgreSQL sites)"
        });
    });

    // Register 7 DbContexts for 7 distributed sites
    builder.Services.AddDbContext<LopK1DbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("LopK1DB")));

    builder.Services.AddDbContext<LopK2DbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("LopK2DB")));

    builder.Services.AddDbContext<SinhVienK1DbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("SinhVienK1DB")));

    builder.Services.AddDbContext<SinhVienK2DbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("SinhVienK2DB")));

    builder.Services.AddDbContext<DangKyDiem1DbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DangKyDiem1DB")));

    builder.Services.AddDbContext<DangKyDiem23K1DbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DangKyDiem23K1DB")));

    builder.Services.AddDbContext<DangKyDiem23K2DbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DangKyDiem23K2DB")));

    // Register Services
    builder.Services.AddScoped<StudentService>();
    builder.Services.AddScoped<ClassService>();
    builder.Services.AddScoped<RegistrationService>();
    builder.Services.AddScoped<AdminService>();
    builder.Services.AddScoped<ReportService>();

    // Add Health Checks for all 7 sites
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("LopK1DB")!, name: "LopK1DB")
        .AddNpgSql(builder.Configuration.GetConnectionString("LopK2DB")!, name: "LopK2DB")
        .AddNpgSql(builder.Configuration.GetConnectionString("SinhVienK1DB")!, name: "SinhVienK1DB")
        .AddNpgSql(builder.Configuration.GetConnectionString("SinhVienK2DB")!, name: "SinhVienK2DB")
        .AddNpgSql(builder.Configuration.GetConnectionString("DangKyDiem1DB")!, name: "DangKyDiem1DB")
        .AddNpgSql(builder.Configuration.GetConnectionString("DangKyDiem23K1DB")!, name: "DangKyDiem23K1DB")
        .AddNpgSql(builder.Configuration.GetConnectionString("DangKyDiem23K2DB")!, name: "DangKyDiem23K2DB");

    // CORS (if needed)
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Distributed DB API v1");
            c.RoutePrefix = string.Empty; // Swagger UI at root
        });
    }

    app.UseSerilogRequestLogging();

    app.UseCors();

    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoint
    app.MapHealthChecks("/health");

    // Simple info endpoint
    app.MapGet("/", () => new
    {
        service = "Distributed Database API",
        version = "1.0.0",
        sites = 7,
        swagger = "/swagger",
        health = "/health",
        endpoints = new[]
        {
            "GET /api/admin/departments",
            "GET /api/admin/overview",
            "GET /api/admin/sites/health",
            "GET /api/admin/config",
            "GET /api/classes",
            "GET /api/classes/{mslop}",
            "GET /api/classes/{mslop}/students",
            "GET /api/students/{mssv}",
            "GET /api/students/{mssv}/khoa",
            "GET /api/students/search",
            "GET /api/registrations/students/{mssv}/scores",
            "GET /api/registrations/subjects/{msmon}/students"
        }
    }).WithName("ApiInfo").WithOpenApi();

    Log.Information("Starting Distributed Database API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
