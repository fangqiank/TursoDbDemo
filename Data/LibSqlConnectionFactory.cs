using Nelknet.LibSQL.Data;

namespace TursoDbDemo.Data;

/// <summary>libSQL 连接工厂。</summary>
public interface ILibSqlConnectionFactory
{
    /// <summary>获取共享的 <see cref="LibSQLConnection"/>（已由 <see cref="EnsureOpenAsync"/> 打开）。</summary>
    LibSQLConnection Create();

    /// <summary>确保共享连接已打开（幂等）。</summary>
    Task EnsureOpenAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 默认实现：维护一个全局复用的 <see cref="LibSQLConnection"/>。
/// </summary>
/// <remarks>
/// 由 <see cref="TursoOptions.DataSource"/> 与 <see cref="TursoOptions.AuthToken"/> 拼接连接字符串：
/// 本地文件模式（无 AuthToken）→ <c>Data Source=app.db</c>；
/// 云端模式（有 AuthToken）→ <c>Data Source=libsql://...;Auth Token=...</c>。
/// <para>Nelknet 本地文件模式下，每次 <c>Open()</c> 都会打开独立文件句柄，多句柄交错访问同一文件会触发 SQLite 的 <c>database is locked</c>，故复用单连接。</para>
/// <para>单连接非线程安全，多请求并发需序列化访问（见 <see cref="Services.ProductService"/> 内的信号量）。</para>
/// </remarks>
public class LibSqlConnectionFactory(TursoOptions options) : ILibSqlConnectionFactory
{
    private readonly LibSQLConnection _connection = new(BuildConnectionString(options));
    private readonly SemaphoreSlim _openGate = new(1, 1);
    private bool _isOpen;

    /// <summary>由 DataSource + AuthToken 拼接 libSQL 连接字符串。</summary>
    private static string BuildConnectionString(TursoOptions options)
    {
        var ds = $"Data Source={options.DataSource}";
        return string.IsNullOrWhiteSpace(options.AuthToken)
            ? ds
            : $"{ds};Auth Token={options.AuthToken}";
    }

    /// <inheritdoc />
    public LibSQLConnection Create() => _connection;

    /// <inheritdoc />
    public async Task EnsureOpenAsync(CancellationToken cancellationToken = default)
    {
        if (_isOpen)
        {
            return;
        }

        await _openGate.WaitAsync(cancellationToken);
        try
        {
            if (!_isOpen)
            {
                await _connection.OpenAsync(cancellationToken);
                _isOpen = true;
            }
        }
        finally
        {
            _openGate.Release();
        }
    }
}
