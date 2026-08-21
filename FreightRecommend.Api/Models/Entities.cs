namespace FreightRecommend.Api.Models;

/// <summary>海运段运费明细（freight_rate_detail）—— 单一数据源</summary>
public class FreightRateDetail
{
    public int Id { get; set; }
    public string OriginPort { get; set; } = "";
    public string DestPort { get; set; } = "";
    public string Carrier { get; set; } = "";
    public string ContainerType { get; set; } = "";
    public string Category { get; set; } = "";
    public string FeeCode { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly EffectiveTo { get; set; }
}

/// <summary>厂址→起运港 国内段运费（factory_freight_rate）</summary>
public class FactoryFreightRate
{
    public int Id { get; set; }
    public string FactoryCode { get; set; } = "";
    public string OriginPort { get; set; } = "";
    public string ContainerType { get; set; } = "";
    public decimal Amount { get; set; }
    public string ChargeMethod { get; set; } = "";
    public string Currency { get; set; } = "CNY";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly EffectiveTo { get; set; }
}

/// <summary>厂址主数据（factory）</summary>
public class Factory
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";
    public string DefOriginPort { get; set; } = "";
    public string Status { get; set; } = "";
}

/// <summary>柜型主数据（config_container）—— 含容积与最大载重</summary>
public class ConfigContainer
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal? VolumeCbm { get; set; }
    public decimal? MaxWeightKg { get; set; }
    public string Note { get; set; } = "";
}

/// <summary>推荐引擎的可配置项（对应伪代码中的全局配置）</summary>
public class RecommendOptions
{
    public const decimal DefaultFxRate = 7.2m;
    public decimal FxRate { get; set; } = DefaultFxRate;
    public decimal LclBreakevenCbm { get; set; } = 15m;
    public DefaultDomestic? DefaultDomestic { get; set; }
}

public record DefaultDomestic(string Factory, string Origin, string Ct);
