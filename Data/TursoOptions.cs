namespace TursoDbDemo.Data;

/// <summary>
/// 绑定 "Turso" 节的连接配置（URL 与 Token 分离）。
/// </summary>
/// <remarks>
/// <para>· <see cref="DataSource"/>：libSQL/Turso 的 URL，或本地文件路径（如 <c>app.db</c>，默认）。</para>
/// <para>· <see cref="AuthToken"/>：访问令牌（仅云端需要；本地文件模式留空）。</para>
/// <para><b>本地开发</b>：云端凭据通过 User Secrets 注入（<c>Turso:DataSource</c>、<c>Turso:AuthToken</c>），不入 appsettings。</para>
/// <para><b>生产环境</b>：用环境变量 <c>Turso__DataSource</c> / <c>Turso__AuthToken</c> 覆盖（User Secrets 仅 Development 加载）。</para>
/// </remarks>
public class TursoOptions
{
    /// <summary>数据源：libSQL URL（如 libsql://xxx.turso.io）或本地文件路径（如 app.db，默认）。</summary>
    public string DataSource { get; set; } = "app.db";

    /// <summary>访问令牌（云端必填；本地文件模式留空）。请勿硬编码，走 User Secrets / 环境变量。</summary>
    public string? AuthToken { get; set; }
}
