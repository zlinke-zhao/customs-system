using FreightRecommend.Api.Data;
using FreightRecommend.Api.Models;

namespace FreightRecommend.Api.Services;

/// <summary>
/// 智能柜型推荐引擎。
/// 逻辑与伪代码文档《运费推荐接口_伪代码.md》逐条对应：
///   1) 海运段回退链：精确柜型 → 20GP → 同航线任意柜型
///   2) 国内段默认档回退：精确 → DefaultDomestic
///   3) 散货 LCL 海运价缺失 → 示例推导（20GP 总价 ÷ lclBreakevenCbm）
///   4) CNY ÷ fxRate 折 USD 后横向比较，取最便宜
///   5) 重量校验：kg > config_container.max_weight
/// </summary>
public class FreightRecommendService(IFreightRepository repo, RecommendOptions options)
{
    private const string PendingOrigin = "（待补充）";

    public RecommendResponse Recommend(RecommendRequest req)
    {
        if (req.Cbm <= 0)
            throw new ArgumentException("cbm 必须大于 0");

        var fx = req.FxRate ?? options.FxRate;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var candidates = new List<Candidate>();

        // 整柜档：20GP + 40HQ
        foreach (var ct in new[] { "20GP", "40HQ" })
        {
            var (sea, seaDemo, _) = SeaFreight(req.OriginPort, req.DestPort, req.Carrier, ct, today);
            var (dom, domFb) = DomesticFreight(req.FactoryCode, req.OriginPort, ct, today);
            candidates.Add(Build(sea, dom, ct, seaDemo, domFb, fx));
        }

        // 散货档（依赖真实 LCL 海运价；缺失则示例推导）
        if (req.IncludeLcl)
            candidates.Add(LclCandidate(req.FactoryCode, req.OriginPort, req.DestPort, req.Carrier, req.Cbm, today, fx));

        // 统一折算为 USD 并比较
        foreach (var c in candidates)
            c.TotalUsd = c.SeaUsd + c.DomesticCny / fx;

        var best = candidates.MinBy(c => c.TotalUsd)
                   ?? throw new InvalidOperationException("无候选方案");

        foreach (var c in candidates)
        {
            c.Recommended = c == best;
            var mw = repo.Container(c.ContainerType)?.MaxWeightKg;
            c.OverWeight = req.Kg > 0 && mw.HasValue && req.Kg > mw.Value;
        }

        var warnings = new List<string>();
        if (req.IncludeLcl && candidates.Any(c => c.ContainerType.StartsWith("LCL") && c.IsDemo))
            warnings.Add($"散货(LCL)海运价为示例推导，盈亏平衡点约 {options.LclBreakevenCbm} CBM；" +
                         "录入真实散货海运价后结论会更准");

        return new RecommendResponse(
            best.ContainerType,
            req.IncludeLcl ? (int?)options.LclBreakevenCbm : null,
            candidates,
            warnings);
    }

    // ---------- 海运段（含默认档回退） ----------
    private (decimal Amount, bool IsDemo, string? FallbackCt) SeaFreight(
        string origin, string dest, string carrier, string? ct, DateOnly today)
    {
        var rows = repo.SeaRates(origin, dest, carrier, ct, today);
        if (rows.Count > 0)
            return (rows.Sum(r => r.Amount), false, null);

        // 回退 1：同航线 20GP
        var fb = repo.SeaRates(origin, dest, carrier, "20GP", today);
        if (fb.Count > 0)
            return (fb.Sum(r => r.Amount), true, "20GP");

        // 回退 2：同航线任意柜型
        var fb2 = repo.SeaRates(origin, dest, carrier, null, today);
        if (fb2.Count > 0)
            return (fb2.Sum(r => r.Amount), true, fb2[0].ContainerType);

        return (0m, true, null);
    }

    // ---------- 国内段（含默认档回退） ----------
    private (decimal Amount, bool IsFallback) DomesticFreight(
        string factoryCode, string originPort, string ct, DateOnly today)
    {
        var rows = repo.DomesticRates(factoryCode, originPort, ct, today);
        if (rows.Count > 0)
            return (rows.Sum(r => r.Amount), false);

        if (options.DefaultDomestic is not null)
        {
            var d = repo.DomesticRates(
                options.DefaultDomestic.Factory, options.DefaultDomestic.Origin, options.DefaultDomestic.Ct, today);
            if (d.Count > 0)
                return (d.Sum(r => r.Amount), true);
        }
        return (0m, true);
    }

    // ---------- 散货 LCL 候选（核心：缺失价 → 示例推导） ----------
    private Candidate LclCandidate(
        string factoryCode, string originPort, string destPort, string carrier,
        decimal cbm, DateOnly today, decimal fx)
    {
        // 海运段
        var lclRows = repo.SeaRates(originPort, destPort, carrier, "LCL(散货拼箱)", today);
        decimal seaRate;
        bool seaDemo;
        if (lclRows.Count > 0)
        {
            seaRate = lclRows.Sum(r => r.Amount);   // 已按 CBM 计的总额
            seaDemo = false;
        }
        else
        {
            var (sea20, _, _) = SeaFreight(originPort, destPort, carrier, "20GP", today);
            seaRate = sea20 / options.LclBreakevenCbm;   // 示例推导：每 CBM 单价
            seaDemo = true;
        }
        var seaUsd = seaRate * cbm;

        // 国内段（散货按 CBM）
        var domRows = repo.DomesticRates(factoryCode, originPort, "LCL(散货拼箱)", today);
        decimal domRate;
        bool domFb;
        if (domRows.Count > 0)
        {
            domRate = domRows.Sum(r => r.Amount);   // 按 CBM 总额
            domFb = false;
        }
        else
        {
            var (dom20, _) = DomesticFreight(factoryCode, originPort, "20GP", today);
            var v20 = repo.Container("20GP")?.VolumeCbm ?? 33.2m;
            domRate = dom20 / v20;                   // 默认档 ÷ 20GP 容积 → 每 CBM
            domFb = true;
        }
        var domCny = domRate * cbm;

        return Build(seaUsd, domCny, "LCL(散货拼箱)", seaDemo, domFb, fx, seaDemo);
    }

    // ---------- 工具 ----------
    private Candidate Build(decimal seaUsd, decimal domCny, string ct,
        bool seaDemo, bool domFb, decimal fx, bool seaDemoNote = false)
    {
        var note = seaDemoNote
            ? $"示例海运价（按 20GP 总价÷{options.LclBreakevenCbm}CBM 推导）"
            : "";
        return new Candidate
        {
            ContainerType = ct,
            SeaUsd = Math.Round(seaUsd),
            DomesticCny = Math.Round(domCny),
            TotalUsd = Math.Round(seaUsd + domCny / fx),
            IsDemo = seaDemo,
            IsFallback = domFb,
            Note = note
        };
    }
}
