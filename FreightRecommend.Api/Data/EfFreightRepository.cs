// 启用本文件：在 .csproj 增加 EF Core 包，并定义编译符号 EFCORE（如 <DefineConstants>EFCORE</DefineConstants>）
// 默认不编译，避免缺少 Microsoft.EntityFrameworkCore 包导致骨架无法构建。
#if EFCORE
using FreightRecommend.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FreightRecommend.Api.Data;

/// <summary>
/// 生产环境仓储（EF Core 版）。
/// 启用步骤：
///   1) 在 .csproj 加入 Microsoft.EntityFrameworkCore.SqlServer（或 Pomelo.MySql）
///   2) 在 Program.cs 中替换注册：
///        builder.Services.AddDbContext<FreightDbContext>(o => o.UseSqlServer(conn));
///        builder.Services.AddScoped<IFreightRepository, EfFreightRepository>();
/// </summary>
public class FreightDbContext : DbContext
{
    public FreightDbContext(DbContextOptions<FreightDbContext> o) : base(o) { }
    public DbSet<FreightRateDetail> FreightRateDetails => Set<FreightRateDetail>();
    public DbSet<FactoryFreightRate> FactoryFreightRates => Set<FactoryFreightRate>();
    public DbSet<Factory> Factories => Set<Factory>();
    public DbSet<ConfigContainer> ConfigContainers => Set<ConfigContainer>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<FreightRateDetail>().HasKey(x => x.Id);
        mb.Entity<FactoryFreightRate>().HasKey(x => x.Id);
        mb.Entity<ConfigContainer>().HasKey(x => x.Code);
    }
}

public class EfFreightRepository(FreightDbContext db) : IFreightRepository
{
    public IReadOnlyList<FreightRateDetail> SeaRates(string origin, string dest, string carrier, string? ct, DateOnly today)
        => db.FreightRateDetails
            .Where(r => r.DestPort == dest && r.Carrier == carrier
                     && (ct == null || r.ContainerType == ct)
                     && (r.OriginPort == origin || r.OriginPort == "（待补充）")
                     && r.EffectiveFrom <= today && r.EffectiveTo >= today)
            .ToList();

    public IReadOnlyList<FactoryFreightRate> DomesticRates(string factoryCode, string originPort, string ct, DateOnly today)
        => db.FactoryFreightRates
            .Where(r => r.FactoryCode == factoryCode && r.OriginPort == originPort && r.ContainerType == ct
                     && r.EffectiveFrom <= today && r.EffectiveTo >= today)
            .ToList();

    public ConfigContainer? Container(string ct)
        => db.ConfigContainers.FirstOrDefault(c => c.Code == ct);
}
#endif
