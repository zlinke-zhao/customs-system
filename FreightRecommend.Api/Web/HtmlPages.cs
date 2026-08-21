namespace FreightRecommend.Api.Web;

/// <summary>
/// 自包含 HTML 页面（无外部依赖、无 CDN，离线可用）。
///   Landing()  —— 根路径首页：服务状态 + 接口清单 + 在线测试控制台
///   Error()    —— 统一错误页（404 / 500 等）
/// 说明：浏览器访问返回本页面；程序化调用（Accept 非 text/html）仍返回 JSON。
/// </summary>
public static class HtmlPages
{
    // ---------------- 公共样式 ----------------
    private const string BaseCss = """
    *{margin:0;padding:0;box-sizing:border-box}
    body{
      font-family:-apple-system,BlinkMacSystemFont,"Segoe UI","Microsoft YaHei",sans-serif;
      background:#f6f8fb;color:#17212f;line-height:1.6;
      -webkit-font-smoothing:antialiased;
    }
    .wrap{max-width:960px;margin:0 auto;padding:40px 24px 64px}
    .card{
      background:#fff;border:1px solid #e3e8ef;border-radius:12px;
      box-shadow:0 1px 3px rgba(16,32,64,.04);margin-bottom:20px;overflow:hidden;
    }
    .card-hd{
      padding:16px 20px;border-bottom:1px solid #eef1f6;
      font-size:15px;font-weight:600;color:#17212f;
      display:flex;align-items:center;gap:8px;
    }
    .card-bd{padding:20px}
    .muted{color:#64748b;font-size:13px}
    .badge{
      display:inline-flex;align-items:center;gap:6px;
      padding:4px 10px;border-radius:20px;font-size:12px;font-weight:600;
    }
    .badge-ok{background:#e7f6ee;color:#0f7b46}
    .badge-warn{background:#fef4e6;color:#a55b09}
    .dot{width:6px;height:6px;border-radius:50%;background:currentColor}
    code{
      font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;
      background:#f1f4f9;padding:2px 6px;border-radius:4px;font-size:13px;color:#0f3f8f;
    }
    table{width:100%;border-collapse:collapse;font-size:13px}
    th,td{padding:10px 12px;text-align:left;border-bottom:1px solid #eef1f6}
    th{background:#f8fafc;font-weight:600;color:#475569;font-size:12px}
    tr:last-child td{border-bottom:none}
    a{color:#1d4ed8;text-decoration:none}
    a:hover{text-decoration:underline}
    """;

    // ---------------- 首页 ----------------
    public static string Landing(string version, decimal fxRate, decimal breakeven, string defaultDomestic) =>
        """
        <!DOCTYPE html>
        <html lang="zh-CN">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>FreightRecommend API · 智能柜型推荐服务</title>
        <style>
        __CSS__
        .hero{
          background:linear-gradient(135deg,#0f3f8f 0%,#1d4ed8 100%);
          color:#fff;border-radius:12px;padding:32px;margin-bottom:24px;
        }
        .hero h1{font-size:24px;font-weight:700;margin-bottom:6px;letter-spacing:-.2px}
        .hero p{opacity:.88;font-size:14px}
        .hero .badge{background:rgba(255,255,255,.18);color:#fff;margin-bottom:14px}
        .meta{display:flex;gap:28px;margin-top:20px;flex-wrap:wrap}
        .meta div{font-size:12px;opacity:.85}
        .meta b{display:block;font-size:16px;font-weight:600;opacity:1;margin-top:2px}
        .ep{
          display:flex;align-items:center;gap:12px;padding:12px 0;
          border-bottom:1px solid #eef1f6;font-size:13px;
        }
        .ep:last-child{border-bottom:none}
        .verb{
          font-family:ui-monospace,monospace;font-size:11px;font-weight:700;
          padding:3px 8px;border-radius:4px;min-width:48px;text-align:center;
        }
        .verb-post{background:#e8f0fe;color:#1249c4}
        .verb-get{background:#e7f6ee;color:#0f7b46}
        .grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:14px}
        label{display:block;font-size:12px;font-weight:600;color:#475569;margin-bottom:5px}
        input,select{
          width:100%;padding:8px 10px;border:1px solid #d8dfe9;border-radius:7px;
          font-size:13px;font-family:inherit;color:#17212f;background:#fff;
        }
        input:focus,select:focus{outline:none;border-color:#1d4ed8;box-shadow:0 0 0 3px rgba(29,78,216,.1)}
        .row-chk{display:flex;align-items:center;gap:8px;margin-top:16px}
        .row-chk input{width:auto}
        .row-chk label{margin:0;font-weight:500;color:#17212f}
        button{
          margin-top:18px;padding:10px 22px;background:#1d4ed8;color:#fff;border:none;
          border-radius:7px;font-size:14px;font-weight:600;cursor:pointer;font-family:inherit;
        }
        button:hover{background:#1743b8}
        button:disabled{background:#94a3b8;cursor:not-allowed}
        #out{margin-top:20px;display:none}
        .best{
          background:#e7f6ee;border:1px solid #b6e2c9;border-radius:8px;
          padding:14px 16px;margin-bottom:14px;
        }
        .best span{font-size:12px;color:#0f7b46;font-weight:600}
        .best b{display:block;font-size:20px;color:#0b5e35;margin-top:2px}
        .win{background:#f0fdf5}
        .warn-box{
          background:#fef8ec;border:1px solid #f5dcae;border-radius:8px;
          padding:12px 14px;margin-top:14px;font-size:12px;color:#8a4d05;
        }
        .err-box{
          background:#fdecec;border:1px solid #f5c2c2;border-radius:8px;
          padding:12px 14px;font-size:13px;color:#a11c1c;
        }
        footer{text-align:center;color:#94a3b8;font-size:12px;margin-top:32px}
        </style>
        </head>
        <body>
        <div class="wrap">

          <div class="hero">
            <span class="badge"><i class="dot"></i>服务运行中</span>
            <h1>FreightRecommend API</h1>
            <p>海运智能柜型推荐服务 · 整柜与散货自动比价</p>
            <div class="meta">
              <div>汇率 (USD→CNY)<b>__FX__</b></div>
              <div>散货盈亏平衡点<b>__BE__ CBM</b></div>
              <div>国内段默认档<b>__DD__</b></div>
              <div>版本<b>__VER__</b></div>
            </div>
          </div>

          <div class="card">
            <div class="card-hd">可用接口</div>
            <div class="card-bd" style="padding:8px 20px">
              <div class="ep"><span class="verb verb-post">POST</span><code>/api/quotation/recommend</code><span class="muted">推荐计算主接口</span></div>
              <div class="ep"><span class="verb verb-get">GET</span><code>/api/quotation/recommend</code><span class="muted">用法说明</span></div>
              <div class="ep"><span class="verb verb-get">GET</span><code>/health</code><span class="muted">健康检查</span></div>
            </div>
          </div>

          <div class="card">
            <div class="card-hd">在线测试控制台</div>
            <div class="card-bd">
              <div class="grid">
                <div><label>厂址编码</label><input id="f_factory" value="F001"></div>
                <div><label>起运港</label><input id="f_origin" value="深圳(盐田)"></div>
                <div><label>目的港</label><input id="f_dest" value="曼萨尼约"></div>
                <div><label>海运公司</label><input id="f_carrier" value="恒荣达"></div>
                <div><label>体积 CBM</label><input id="f_cbm" type="number" step="0.1" value="8"></div>
                <div><label>重量 KG</label><input id="f_kg" type="number" step="1" value="5000"></div>
              </div>
              <div class="row-chk">
                <input type="checkbox" id="f_lcl" checked>
                <label for="f_lcl">纳入散货 (LCL) 对比</label>
              </div>
              <button id="btn" onclick="run()">计算推荐方案</button>

              <div id="out"></div>
            </div>
          </div>

          <footer>FreightRecommend.Api · ASP.NET Core · 演示数据请在上线前替换为真实报价</footer>
        </div>

        <script>
        function esc(s){return String(s).replace(/[&<>]/g,function(c){return {'&':'&amp;','<':'&lt;','>':'&gt;'}[c]})}
        function money(n){return Number(n).toLocaleString('zh-CN')}

        async function run(){
          var btn=document.getElementById('btn'), out=document.getElementById('out');
          btn.disabled=true; btn.textContent='计算中…'; out.style.display='none';
          var body={
            factoryCode:document.getElementById('f_factory').value,
            originPort :document.getElementById('f_origin').value,
            destPort   :document.getElementById('f_dest').value,
            carrier    :document.getElementById('f_carrier').value,
            cbm        :parseFloat(document.getElementById('f_cbm').value)||0,
            kg         :parseFloat(document.getElementById('f_kg').value)||0,
            includeLcl :document.getElementById('f_lcl').checked
          };
          try{
            var res=await fetch('/api/quotation/recommend',{
              method:'POST',
              headers:{'Content-Type':'application/json'},
              body:JSON.stringify(body)
            });
            var data=await res.json();
            if(!res.ok){
              out.innerHTML='<div class="err-box">请求失败 ('+res.status+')：'+esc(data.error||JSON.stringify(data))+'</div>';
            }else{
              out.innerHTML=render(data);
            }
          }catch(e){
            out.innerHTML='<div class="err-box">网络异常：'+esc(e.message)+'</div>';
          }
          out.style.display='block';
          btn.disabled=false; btn.textContent='计算推荐方案';
        }

        function render(d){
          var h='<div class="best"><span>推荐方案</span><b>'+esc(d.bestContainerType)+'</b></div>';
          h+='<table><thead><tr><th>柜型</th><th>海运段 USD</th><th>国内段 CNY</th><th>折算总价 USD</th><th>状态</th></tr></thead><tbody>';
          (d.candidates||[]).forEach(function(c){
            var tags=[];
            if(c.recommended) tags.push('<span class="badge badge-ok">推荐</span>');
            if(c.isDemo)      tags.push('<span class="badge badge-warn">示例价</span>');
            if(c.isFallback)  tags.push('<span class="badge badge-warn">默认档</span>');
            if(c.overWeight)  tags.push('<span class="badge badge-warn">超重</span>');
            h+='<tr class="'+(c.recommended?'win':'')+'">'
              +'<td><b>'+esc(c.containerType)+'</b></td>'
              +'<td>'+money(c.seaUsd)+'</td>'
              +'<td>'+money(c.domesticCny)+'</td>'
              +'<td><b>'+money(c.totalUsd)+'</b></td>'
              +'<td>'+(tags.join(' ')||'—')+'</td></tr>';
          });
          h+='</tbody></table>';
          if(d.lclBreakevenCbm) h+='<p class="muted" style="margin-top:10px">散货/整柜盈亏平衡点参考：'+d.lclBreakevenCbm+' CBM</p>';
          (d.warnings||[]).forEach(function(w){ h+='<div class="warn-box">⚠ '+esc(w)+'</div>'; });
          return h;
        }
        </script>
        </body>
        </html>
        """
        .Replace("__CSS__", BaseCss)
        .Replace("__FX__", fxRate.ToString("0.##"))
        .Replace("__BE__", breakeven.ToString("0.##"))
        .Replace("__DD__", defaultDomestic)
        .Replace("__VER__", version);

    // ---------------- 错误页 ----------------
    public static string Error(int code, string title, string message, string path) =>
        """
        <!DOCTYPE html>
        <html lang="zh-CN">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>__CODE__ · __TITLE__</title>
        <style>
        __CSS__
        .wrap{max-width:600px;padding-top:80px}
        .panel{background:#fff;border:1px solid #e3e8ef;border-radius:12px;padding:40px;text-align:center;
               box-shadow:0 1px 3px rgba(16,32,64,.04)}
        .code{font-size:64px;font-weight:800;color:#1d4ed8;line-height:1;letter-spacing:-2px}
        .code.s5{color:#c2410c}
        h1{font-size:20px;font-weight:700;margin:14px 0 8px}
        .desc{color:#64748b;font-size:14px;margin-bottom:22px}
        .path{
          background:#f1f4f9;border-radius:7px;padding:10px 14px;margin-bottom:26px;
          font-family:ui-monospace,monospace;font-size:12px;color:#475569;
          word-break:break-all;text-align:left;
        }
        .path span{color:#94a3b8;display:block;font-size:11px;margin-bottom:2px;font-family:inherit}
        .btn{
          display:inline-block;padding:10px 24px;background:#1d4ed8;color:#fff;
          border-radius:7px;font-size:14px;font-weight:600;
        }
        .btn:hover{background:#1743b8;text-decoration:none}
        .eps{margin-top:28px;padding-top:22px;border-top:1px solid #eef1f6;text-align:left}
        .eps p{font-size:12px;font-weight:600;color:#475569;margin-bottom:10px}
        .eps div{font-size:12px;color:#64748b;padding:4px 0}
        </style>
        </head>
        <body>
        <div class="wrap">
          <div class="panel">
            <div class="code __S5__">__CODE__</div>
            <h1>__TITLE__</h1>
            <p class="desc">__MSG__</p>
            <div class="path"><span>请求地址</span>__PATH__</div>
            <a class="btn" href="/">返回服务首页</a>

            <div class="eps">
              <p>可用接口</p>
              <div><code>POST</code> /api/quotation/recommend — 推荐计算</div>
              <div><code>GET</code> &nbsp;/api/quotation/recommend — 用法说明</div>
              <div><code>GET</code> &nbsp;/health — 健康检查</div>
            </div>
          </div>
        </div>
        </body>
        </html>
        """
        .Replace("__CSS__", BaseCss)
        .Replace("__S5__", code >= 500 ? "s5" : "")
        .Replace("__CODE__", code.ToString())
        .Replace("__TITLE__", Escape(title))
        .Replace("__MSG__", Escape(message))
        .Replace("__PATH__", Escape(string.IsNullOrEmpty(path) ? "/" : path));

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
