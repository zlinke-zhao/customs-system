# FreightRecommend.Api — 智能柜型推荐后端骨架

> 基于《运费推荐接口_伪代码.md》落地的可运行 ASP.NET Core (net8.0) 最小 API。
> 开箱即用（内存种子数据），带服务首页与统一错误页，生产环境可一键切换 EF Core。

---

## 1. 快速运行

```bash
cd FreightRecommend.Api
dotnet run
```

`Properties/launchSettings.json` 已固定端口并自动打开浏览器，启动后直接访问：

**http://localhost:5099**

> 如需手动指定端口：`dotnet run --urls http://localhost:5099`

### 端口打不开？按此排查

| 现象 | 原因 | 处理 |
|------|------|------|
| 浏览器"无法访问此页面" | 旧进程占用端口，新实例启动失败 | `Get-Process FreightRecommend.Api \| Stop-Process -Force` 后重启 |
| 页面空白 / 404 | 访问了未映射的路径 | 访问根路径 `/`，会显示服务首页 |
| 构建报 MSB3027 文件被锁定 | 服务仍在运行 | 先停进程再 `dotnet build` |

---

## 2. 页面与错误处理

服务不再是"裸 API"，浏览器访问有完整页面：

| 路径 | 说明 |
|------|------|
| `GET /` | **服务首页**：运行状态、当前配置（汇率/盈亏平衡点/默认档）、接口清单、**在线测试控制台**（可直接填表调用推荐接口并渲染结果表格） |
| `GET /health` | 健康检查，返回 JSON |
| 任意未匹配路径 | **404 错误页**：状态码、原因说明、请求地址、返回首页按钮、可用接口列表 |
| 未捕获异常 | **500 错误页**：Development 显示异常详情，Production 只显示友好提示（不泄露堆栈） |
| `GET /dev/throw` | 仅 Development 暴露，用于验证 500 页面；Production 自动返回 404 |

### 内容协商（关键设计）

同一个错误，**浏览器看到美观页面，程序拿到结构化 JSON**：

```bash
# 浏览器（Accept: text/html）→ HTML 错误页
curl -H "Accept: text/html" http://localhost:5099/abc/xyz

# 程序调用（Accept: application/json）→ JSON
curl -H "Accept: application/json" http://localhost:5099/abc/xyz
# {"status":404,"error":"页面不存在","message":"你访问的地址没有对应的接口，请检查路径是否正确。","path":"/abc/xyz"}
```

已覆盖状态码：**404** 页面不存在 / **405** 方法不允许 / **415** 内容类型不支持 / **500** 服务器内部错误。
JSON 输出使用宽松编码，中文不会被转义成 `\uXXXX`。

> 页面为自包含 HTML（内联 CSS/JS，无 CDN、无外部依赖），断网与内网环境均正常显示。

---

## 3. 接口契约

### 请求 `POST /api/quotation/recommend`

```json
{
  "factoryCode": "F001",
  "originPort": "深圳(盐田)",
  "destPort": "曼萨尼约",
  "carrier": "恒荣达",
  "cbm": 8,
  "kg": 5000,
  "includeLcl": true,
  "fxRate": 7.2
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| factoryCode | string | 是 | 厂址编码，如 F001 |
| originPort | string | 是 | 起运港，如 深圳(盐田) |
| destPort | string | 是 | 目的港，如 曼萨尼约 |
| carrier | string | 是 | 海运公司，如 恒荣达 |
| cbm | decimal | 是 | 货物体积（m³），必须 > 0 |
| kg | decimal | 否 | 货物重量（kg），用于超重校验，默认 0（不校验） |
| includeLcl | bool | 否 | 是否纳入散货(LCL)对比，默认 false |
| fxRate | decimal | 否 | 汇率覆盖（USD→CNY），缺省用配置值 7.2 |

### 响应

```json
{
  "bestContainerType": "LCL(散货拼箱)",
  "lclBreakevenCbm": 15,
  "candidates": [
    { "containerType": "20GP", "seaUsd": 10263, "domesticCny": 950,  "totalUsd": 10395, "isDemo": false, "isFallback": false, "overWeight": false, "recommended": false, "note": "" },
    { "containerType": "40HQ", "seaUsd": 14560, "domesticCny": 1300, "totalUsd": 14741, "isDemo": false, "isFallback": false, "overWeight": false, "recommended": false, "note": "" },
    { "containerType": "LCL(散货拼箱)", "seaUsd": 5474, "domesticCny": 680, "totalUsd": 5568, "isDemo": true, "isFallback": false, "overWeight": false, "recommended": true, "note": "示例海运价（按 20GP 总价÷15CBM 推导）" }
  ],
  "warnings": ["散货(LCL)海运价为示例推导，盈亏平衡点约 15 CBM；录入真实散货海运价后结论会更准"]
}
```

| 字段 | 说明 |
|------|------|
| bestContainerType | 推荐柜型（总价最低者） |
| lclBreakevenCbm | 散货/整柜盈亏平衡点（CBM），未开启 LCL 时为 null |
| candidates[].seaUsd | 海运段总价（USD） |
| candidates[].domesticCny | 国内段总价（CNY） |
| candidates[].totalUsd | 折算后总价（USD）= seaUsd + domesticCny / fxRate |
| candidates[].isDemo | 海运价是否为示例推导（真实价缺失时的回退） |
| candidates[].isFallback | 国内段是否走了默认档回退 |
| candidates[].overWeight | 是否超过该柜型最大载重 |
| candidates[].recommended | 是否为最终推荐方案 |
| warnings | 提示信息（如示例价告警） |

错误响应（400）：

```json
{ "status": 400, "error": "参数错误", "message": "cbm 必须大于 0" }
```

---

## 4. 核心算法（与伪代码逐条对应）

| 步骤 | 实现位置 | 说明 |
|------|----------|------|
| 海运段回退链 | `SeaFreight()` | 精确柜型 → 同航线 20GP → 同航线任意柜型 |
| 国内段默认档回退 | `DomesticFreight()` | 精确匹配 → 配置的 DefaultDomestic（F001/深圳(盐田)/20GP=950） |
| 散货缺失价推导 | `LclCandidate()` | 无真实 LCL 价时，海运 = 20GP 总价 ÷ 盈亏平衡 CBM（15） |
| 折算比较 | `Recommend()` | 统一折 USD：totalUsd = seaUsd + domesticCny / fx，取最小 |
| 超重校验 | `Recommend()` | kg > config_container.max_weight → overWeight=true |

---

## 5. 已验证用例（实测通过 ✅）

### 推荐算法

| 用例 | 输入 | 预期 | 结果 |
|------|------|------|------|
| 1 小货量 | cbm=8, includeLcl=true | 推荐 LCL | ✅ totalUsd 5568 最低 |
| 2 大货量 | cbm=60, includeLcl=true | 推荐整柜 | ✅ 推荐 20GP，LCL 41763 被淘汰 |
| 3 超重 | cbm=50, kg=30000 | 超重预警 | ✅ 20GP/40HQ 均 overWeight=true |
| 4 未配柜型/厂址 | factoryCode=F999 | 回退默认档 | ✅ 演示不空，国内段走 950 |
| 5 参数校验 | cbm=0 | 400 参数错误 | ✅ 返回"cbm 必须大于 0" |

### 页面与错误页

| 路径 | 预期状态码 | 结果 |
|------|-----------|------|
| `/` | 200 首页 | ✅ 配置值正确注入（7.2 / 15 CBM / F001） |
| `/health` | 200 | ✅ 返回 healthy |
| `/abc/xyz` | 404 错误页 | ✅ HTML 页面 + JSON 双通道均正常 |
| `/dev/throw` (Development) | 500 错误页 | ✅ 显示异常详情 |
| `/dev/throw` (Production) | 404 | ✅ 生产环境不暴露 |

---

## 6. 切换到生产数据库（EF Core）

骨架默认用 `InMemoryFreightRepository`（内存种子，开箱即跑）。接入真实库步骤：

1. **加包**：在 `FreightRecommend.Api.csproj` 取消注释并还原 `Microsoft.EntityFrameworkCore.SqlServer`（或 Pomelo.MySql）。
2. **开启 EF 文件**：`Data/EfFreightRepository.cs` 用 `#if EFCORE` 包裹，在 csproj 加 `<DefineConstants>EFCORE</DefineConstants>`。
3. **切换注册**：在 `Program.cs` 注释掉内存仓储，启用：
   ```csharp
   builder.Services.AddDbContext<FreightDbContext>(o =>
       o.UseSqlServer(builder.Configuration.GetConnectionString("Freight")));
   builder.Services.AddScoped<IFreightRepository, EfFreightRepository>();
   ```
4. **建表映射**：`FreightRateDetail` / `FactoryFreightRate` / `ConfigContainer` 对应原型三张表
   （freight_rate_detail / factory_freight_rate / config_container）。

> 业务算法层（`FreightRecommendService`）与数据源解耦，切库后无需改动。

---

## 7. 目录结构

```
FreightRecommend.Api/
├─ Program.cs                      # 入口：错误处理管线 + 页面 + 接口 + DI
├─ appsettings.json                # 汇率/盈亏平衡点/默认档配置
├─ Properties/
│  └─ launchSettings.json          # 固定端口 5099 + 自动打开浏览器
├─ Models/
│  ├─ RecommendDtos.cs             # 请求/响应 DTO
│  └─ Entities.cs                  # 数据实体 + RecommendOptions
├─ Data/
│  ├─ IFreightRepository.cs        # 仓储接口
│  ├─ InMemoryFreightRepository.cs # 演示内存仓储（种子数据）
│  └─ EfFreightRepository.cs       # 生产 EF 仓储（#if EFCORE，默认不编译）
├─ Services/
│  └─ FreightRecommendService.cs   # 推荐引擎核心算法
└─ Web/
   └─ HtmlPages.cs                 # 服务首页 + 统一错误页（自包含 HTML）
```

---

## 8. 上线前待办 ⚠️

- 所有种子金额为**演示值**，需替换为真实报价，尤其**散货 LCL 海运价**（当前为示例推导，响应带 warning）。
- 建议移除或保留 `/dev/throw`（已限定 Development，生产自动 404）。
- 首页在线测试控制台便于联调，若不希望生产暴露，可在 `MapGet("/")` 外加环境判断。
