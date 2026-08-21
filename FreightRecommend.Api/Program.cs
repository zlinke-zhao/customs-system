using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Diagnostics;
using FreightRecommend.Api.Data;
using FreightRecommend.Api.Models;
using FreightRecommend.Api.Services;
using FreightRecommend.Api.Web;

var builder = WebApplication.CreateBuilder(args);

// ---- 配置：从 appsettings.json 的 "Recommend" 节点绑定 ----
var recommendOpts = new RecommendOptions();
builder.Configuration.GetSection("Recommend").Bind(recommendOpts);
builder.Services.AddSingleton(recommendOpts);

// ---- 仓储：演示用内存仓储（开箱即跑）。
//      生产环境替换为 EF：取消下面两行注释，并注释掉 InMemoryFreightRepository。
// builder.Services.AddDbContext<FreightDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Freight")));
// builder.Services.AddScoped<IFreightRepository, EfFreightRepository>();
builder.Services.AddSingleton<IFreightRepository, InMemoryFreightRepository>();

builder.Services.AddScoped<FreightRecommendService>();

var app = builder.Build();

// ======================================================================
//  错误处理：浏览器访问返回美观 HTML 页面；程序调用返回 JSON（内容协商）
// ======================================================================
static bool WantsHtml(HttpRequest req) =>
    req.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

// 中文不转义，便于直接阅读日志与响应
var jsonOpts = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

// 500：未捕获异常
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;

    if (WantsHtml(ctx.Request))
    {
        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.WriteAsync(HtmlPages.Error(
            500, "服务器内部错误",
            app.Environment.IsDevelopment()
                ? ex?.Message ?? "未知错误"
                : "服务处理请求时发生异常，请联系系统管理员或稍后重试。",
            ctx.Request.Path));
    }
    else
    {
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = 500,
            error = "InternalServerError",
            message = app.Environment.IsDevelopment() ? ex?.Message : "服务器内部错误",
            path = ctx.Request.Path.Value
        }, jsonOpts));
    }
}));

// 404 / 405 等无响应体的状态码
app.UseStatusCodePages(async ctx =>
{
    var res = ctx.HttpContext.Response;
    var req = ctx.HttpContext.Request;

    var (title, msg) = res.StatusCode switch
    {
        404 => ("页面不存在", "你访问的地址没有对应的接口，请检查路径是否正确。"),
        405 => ("请求方法不允许", "该接口不支持当前 HTTP 方法，推荐计算请使用 POST。"),
        415 => ("不支持的内容类型", "请在请求头设置 Content-Type: application/json。"),
        _ => ("请求无法处理", "服务未能完成本次请求。")
    };

    if (WantsHtml(req))
    {
        res.ContentType = "text/html; charset=utf-8";
        await res.WriteAsync(HtmlPages.Error(res.StatusCode, title, msg, req.Path));
    }
    else
    {
        res.ContentType = "application/json; charset=utf-8";
        await res.WriteAsync(JsonSerializer.Serialize(new
        {
            status = res.StatusCode,
            error = title,
            message = msg,
            path = req.Path.Value
        }, jsonOpts));
    }
});

// ======================================================================
//  页面与接口
// ======================================================================

// 根路径：服务首页（状态 + 接口清单 + 在线测试控制台）
app.MapGet("/", () =>
{
    var dd = recommendOpts.DefaultDomestic is null
        ? "未配置"
        : $"{recommendOpts.DefaultDomestic.Factory} / {recommendOpts.DefaultDomestic.Origin} / {recommendOpts.DefaultDomestic.Ct}";

    return Results.Content(
        HtmlPages.Landing("v1.0", recommendOpts.FxRate, recommendOpts.LclBreakevenCbm, dd),
        "text/html; charset=utf-8");
});

// 健康检查
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "FreightRecommend.Api",
    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
}));

// 仅开发环境：用于验证 500 错误页是否正常（生产环境不暴露）
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/throw", IResult () =>
        throw new InvalidOperationException("这是一条用于验证 500 错误页的测试异常"));
}

// ---- 接口：POST /api/quotation/recommend ----
app.MapPost("/api/quotation/recommend", (RecommendRequest req, FreightRecommendService svc) =>
{
    try
    {
        return Results.Ok(svc.Recommend(req));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { status = 400, error = "参数错误", message = ex.Message });
    }
});

// 用法说明
app.MapGet("/api/quotation/recommend", () => Results.Ok(new
{
    usage = "请使用 POST 调用本接口",
    contentType = "application/json",
    body = new
    {
        factoryCode = "F001",
        originPort = "深圳(盐田)",
        destPort = "曼萨尼约",
        carrier = "恒荣达",
        cbm = 8,
        kg = 5000,
        includeLcl = true,
        fxRate = 7.2
    }
}));

app.Run();
