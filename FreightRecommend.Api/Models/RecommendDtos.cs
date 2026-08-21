namespace FreightRecommend.Api.Models;

/// <summary>
/// 智能柜型推荐 - 请求体
/// 对应原型 v1.8「⑥ 报价单生成」的【智能柜型推荐】面板输入。
/// </summary>
public record RecommendRequest(
    string FactoryCode,
    string OriginPort,
    string DestPort,
    string Carrier,
    decimal Cbm,
    decimal Kg = 0,
    bool IncludeLcl = true,
    decimal? FxRate = null);

/// <summary>单档候选方案（散货 / 20GP / 40HQ）</summary>
public class Candidate
{
    public string ContainerType { get; set; } = "";
    public decimal SeaUsd { get; set; }
    public decimal DomesticCny { get; set; }
    public decimal TotalUsd { get; set; }
    public bool Recommended { get; set; }
    public bool IsDemo { get; set; }      // 散货海运价为示例推导（无真实 LCL 价）
    public bool IsFallback { get; set; }   // 国内段走了默认档回退
    public bool OverWeight { get; set; }   // 超过该柜型最大载重
    public string Note { get; set; } = "";
}

/// <summary>推荐响应</summary>
public record RecommendResponse(
    string Recommended,
    int? BreakevenCbm,
    List<Candidate> Candidates,
    List<string> Warnings);
