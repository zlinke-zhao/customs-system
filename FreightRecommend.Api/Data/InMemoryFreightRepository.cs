using FreightRecommend.Api.Models;

namespace FreightRecommend.Api.Data;

/// <summary>
/// 演示用内存仓储：装载与原型一致的种子数据。
/// 真实环境请改用 EfFreightRepository（从 freight_rate_detail / factory_freight_rate / config_container 读取）。
/// </summary>
public class InMemoryFreightRepository : IFreightRepository
{
    private readonly List<FreightRateDetail> _sea;
    private readonly List<FactoryFreightRate> _domestic;
    private readonly List<ConfigContainer> _containers;

    public InMemoryFreightRepository()
    {
        var wide = new DateOnly(2020, 1, 1);
        var far = new DateOnly(2030, 12, 31);

        // 海运段（演示路线：深圳(盐田)/曼萨尼约/恒荣达）
        // 注：金额仅为演示种子；真实值来自 freight_rate_detail 表。
        _sea = new()
        {
            // 20GP（8 行，合计 ≈ 10,263 USD）
            Sea("（待补充）", "曼萨尼约", "恒荣达", "20GP", "海运费", "SEA-01", 6900),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "20GP", "目的港费用", "DES-01", 1200),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "20GP", "目的港费用", "DES-02", 350),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "20GP", "目的港费用", "DES-03", 180),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "20GP", "附加费", "ADD-01", 1500),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "20GP", "附加费", "ADD-02", 1300),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "20GP", "附加费", "ADD-03", 300),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "20GP", "其他费用", "OTH-01", 533),
            // 40HQ（演示：无则推荐引擎回退到 20GP）
            Sea("（待补充）", "曼萨尼约", "恒荣达", "40HQ", "海运费", "SEA-01", 8500),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "40HQ", "目的港费用", "DES-01", 1200),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "40HQ", "目的港费用", "DES-02", 350),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "40HQ", "目的港费用", "DES-03", 180),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "40HQ", "附加费", "ADD-01", 1800),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "40HQ", "附加费", "ADD-02", 1500),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "40HQ", "附加费", "ADD-03", 350),
            Sea("（待补充）", "曼萨尼约", "恒荣达", "40HQ", "其他费用", "OTH-01", 680),
            // 散货 LCL：故意不装载，用于演示"示例推导"分支
        };

        // 国内段（厂址 → 起运港 → 柜型）
        _domestic = new()
        {
            Dom("F001", "深圳(盐田)", "20GP", 950),
            Dom("F001", "深圳(盐田)", "40HQ", 1300),
            Dom("F001", "深圳(盐田)", "LCL(散货拼箱)", 85),   // 按 CBM 计费
            Dom("F002", "深圳(蛇口)", "20GP", 1100),
            Dom("F002", "深圳(蛇口)", "40HQ", 1500),
            Dom("F003", "广州(南沙)", "20GP", 800),
            Dom("F003", "广州(南沙)", "40HQ", 1150),
            Dom("F004", "上海(洋山)", "20GP", 1200),
        };

        _containers = new()
        {
            new() { Code = "20GP", Name = "20尺普通柜", VolumeCbm = 33.2m, MaxWeightKg = 28200m },
            new() { Code = "40GP", Name = "40尺普通柜", VolumeCbm = 67.7m, MaxWeightKg = 26650m },
            new() { Code = "40HQ", Name = "40尺高柜", VolumeCbm = 76.3m, MaxWeightKg = 26650m },
            new() { Code = "45HQ", Name = "45尺高柜", VolumeCbm = 86.0m, MaxWeightKg = 25680m },
            new() { Code = "LCL(散货拼箱)", Name = "散货拼箱", VolumeCbm = null, MaxWeightKg = null },
        };
    }

    public IReadOnlyList<FreightRateDetail> SeaRates(string origin, string dest, string carrier, string? ct, DateOnly today)
        => _sea.Where(r => r.DestPort == dest && r.Carrier == carrier
                        && (ct == null || r.ContainerType == ct)
                        && (r.OriginPort == origin || r.OriginPort == "（待补充）")
                        && r.EffectiveFrom <= today && r.EffectiveTo >= today)
               .ToList();

    public IReadOnlyList<FactoryFreightRate> DomesticRates(string factoryCode, string originPort, string ct, DateOnly today)
        => _domestic.Where(r => r.FactoryCode == factoryCode && r.OriginPort == originPort && r.ContainerType == ct
                             && r.EffectiveFrom <= today && r.EffectiveTo >= today)
               .ToList();

    public ConfigContainer? Container(string ct)
        => _containers.FirstOrDefault(c => c.Code == ct);

    // ---- 种子构造助手 ----
    private static FreightRateDetail Sea(string o, string p, string c, string ct, string cat, string fee, decimal amt)
        => new() { OriginPort = o, DestPort = p, Carrier = c, ContainerType = ct, Category = cat, FeeCode = fee, Amount = amt,
                   Currency = "USD", EffectiveFrom = new(2020,1,1), EffectiveTo = new(2030,12,31) };

    private static FactoryFreightRate Dom(string f, string o, string ct, decimal amt)
        => new() { FactoryCode = f, OriginPort = o, ContainerType = ct, Amount = amt, ChargeMethod = "按柜/按CBM",
                   Currency = "CNY", EffectiveFrom = new(2020,1,1), EffectiveTo = new(2030,12,31) };
}
