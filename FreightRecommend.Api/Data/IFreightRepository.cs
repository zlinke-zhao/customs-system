using System.Collections.Generic;
using FreightRecommend.Api.Models;

namespace FreightRecommend.Api.Data;

/// <summary>
/// 运费数据访问抽象。演示用 InMemoryFreightRepository；
/// 生产环境替换为 EfFreightRepository（见 EfFreightRepository.cs）。
/// </summary>
public interface IFreightRepository
{
    /// <summary>
    /// 海运段明细。origin 软匹配：真实港 + "（待补充）"。
    /// ct 为 null 时表示"同航线任意柜型"（默认档回退第二步）。
    /// </summary>
    IReadOnlyList<FreightRateDetail> SeaRates(string origin, string dest, string carrier, string? ct, DateOnly today);

    /// <summary>国内段明细（厂址 → 起运港 → 柜型）</summary>
    IReadOnlyList<FactoryFreightRate> DomesticRates(string factoryCode, string originPort, string ct, DateOnly today);

    /// <summary>柜型主数据（取最大载重做重量校验）</summary>
    ConfigContainer? Container(string ct);
}
