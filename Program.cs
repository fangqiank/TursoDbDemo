using Microsoft.AspNetCore.Diagnostics;
using Scalar.AspNetCore;
using TursoDbDemo.Data;
using TursoDbDemo.Endpoints;
using TursoDbDemo.Services;

var builder = WebApplication.CreateBuilder(args);

// 绑定 Turso 连接配置（默认本地文件，可改 Turso 云端连接字符串）
var tursoOptions = builder.Configuration.GetSection("Turso").Get<TursoOptions>() ?? new TursoOptions();
builder.Services.AddSingleton(tursoOptions);

// 数据访问与业务服务注册
builder.Services.AddSingleton<ILibSqlConnectionFactory, LibSqlConnectionFactory>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// OpenAPI 文档（开发环境通过 /openapi/v1.json 访问）
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();    
}

// 全局异常处理：把未处理异常统一转为 500 JSON
app.UseExceptionHandler();

// 静态文件托管 TUI 前端（wwwroot/index.html）。
// "/" 与静态资源由此服务；"/api/*" 因无对应文件会继续流转到下方端点。
app.UseDefaultFiles();
app.UseStaticFiles();

// 启动时初始化数据库（建表 + 首次种子数据）
await app.Services.GetRequiredService<DatabaseInitializer>()
    .InitializeAsync(app.Lifetime.ApplicationStopping);

// 商品 CRUD 端点
app.MapProductEndpoints();

app.Run();

/// <summary>全局异常处理器：记录日志并返回统一的错误响应。</summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "未处理的异常: {Message}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new { error = "服务器内部错误，请稍后重试。" }, cancellationToken);
        return true;
    }
}
