using System.Data.Common;
using System.Globalization;
using TursoDbDemo.Data;
using TursoDbDemo.Models;

namespace TursoDbDemo.Services;

/// <summary>
/// 基于 Nelknet.LibSQL.Data（纯 ADO.NET）的 <see cref="IProductService"/> 实现。
/// </summary>
/// <remarks>
/// Nelknet 本地模式下已规避的已知行为：
/// <list type="bullet">
/// <item>参数名必须带 <c>@</c> 前缀（LibSQLParameter 要求），故用原生 ADO.NET 而非 Dapper（Dapper 会去除前缀）。</item>
/// <item>所有写操作显式开启事务并 <c>CommitAsync</c>；写语句后跟随一条 SELECT（<c>last_insert_rowid()</c> / <c>changes()</c>）触发 statement 重置、释放写锁后再提交。</item>
/// <item>复用 <see cref="ILibSqlConnectionFactory"/> 提供的单例连接；因单连接非线程安全，方法内用信号量序列化。</item>
/// </list>
/// </remarks>
public class ProductService(ILibSqlConnectionFactory connectionFactory, ILogger<ProductService> logger)
    : IProductService
{
    private const string SelectColumns = "id, name, description, price_cents, stock, created_at, updated_at";

    /// <summary>序列化对单例连接的访问。</summary>
    

    /// <inheritdoc />
    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await connectionFactory.SerializationGate.WaitAsync(cancellationToken);
        try
        {
            var connection = connectionFactory.Create();
            await connectionFactory.EnsureOpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {SelectColumns} FROM products ORDER BY id;";

            var list = new List<Product>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(Map(reader));
            }
            return list;
        }
        finally
        {
            connectionFactory.SerializationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await connectionFactory.SerializationGate.WaitAsync(cancellationToken);
        try
        {
            var connection = connectionFactory.Create();
            await connectionFactory.EnsureOpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {SelectColumns} FROM products WHERE id = @Id;";
            command.AddParam("@Id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        finally
        {
            connectionFactory.SerializationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Product> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        await connectionFactory.SerializationGate.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow.ToString("O");
            var connection = connectionFactory.Create();
            await connectionFactory.EnsureOpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            long newId;
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO products (name, description, price_cents, stock, created_at, updated_at)
                    VALUES (@Name, @Description, @Price, @Stock, @CreatedAt, @UpdatedAt);
                    """;
                command.AddParam("@Name", dto.Name);
                command.AddParam("@Description", dto.Description);
                command.AddParam("@Price", (long)(dto.Price * 100m));
                command.AddParam("@Stock", dto.Stock);
                command.AddParam("@CreatedAt", now);
                command.AddParam("@UpdatedAt", now);
                await command.ExecuteNonQueryAsync(cancellationToken);

                // 跟随 SELECT 取自增主键，同时重置 statement 释放写锁
                command.Parameters.Clear();
                command.CommandText = "SELECT last_insert_rowid();";
                newId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("已创建商品 id={Id} name={Name}", newId, dto.Name);
            return await GetByIdAsyncCore(newId, cancellationToken)
                ?? throw new InvalidOperationException("创建后无法读取记录");
        }
        finally
        {
            connectionFactory.SerializationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Product?> UpdateAsync(long id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        await connectionFactory.SerializationGate.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow.ToString("O");
            var connection = connectionFactory.Create();
            await connectionFactory.EnsureOpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            int affected;
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    UPDATE products
                    SET name = @Name,
                        description = @Description,
                        price_cents = @Price,
                        stock = @Stock,
                        updated_at = @UpdatedAt
                    WHERE id = @Id;
                    """;
                command.AddParam("@Id", id);
                command.AddParam("@Name", dto.Name);
                command.AddParam("@Description", dto.Description);
                command.AddParam("@Price", (long)(dto.Price * 100m));
                command.AddParam("@Stock", dto.Stock);
                command.AddParam("@UpdatedAt", now);
                await command.ExecuteNonQueryAsync(cancellationToken);

                command.Parameters.Clear();
                command.CommandText = "SELECT changes();";
                affected = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            await transaction.CommitAsync(cancellationToken);

            if (affected == 0)
            {
                return null;
            }

            logger.LogInformation("已更新商品 id={Id}", id);
            return await GetByIdAsyncCore(id, cancellationToken);
        }
        finally
        {
            connectionFactory.SerializationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await connectionFactory.SerializationGate.WaitAsync(cancellationToken);
        try
        {
            var connection = connectionFactory.Create();
            await connectionFactory.EnsureOpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            int affected;
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM products WHERE id = @Id;";
                command.AddParam("@Id", id);
                await command.ExecuteNonQueryAsync(cancellationToken);

                command.Parameters.Clear();
                command.CommandText = "SELECT changes();";
                affected = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            await transaction.CommitAsync(cancellationToken);

            if (affected > 0)
            {
                logger.LogInformation("已删除商品 id={Id}", id);
            }
            return affected > 0;
        }
        finally
        {
            connectionFactory.SerializationGate.Release();
        }
    }

    /// <summary>不带锁的内部读取（供已持锁的 Create/Update 复用，避免重入死锁）。</summary>
    private async Task<Product?> GetByIdAsyncCore(long id, CancellationToken cancellationToken)
    {
        var connection = connectionFactory.Create();
        await connectionFactory.EnsureOpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {SelectColumns} FROM products WHERE id = @Id;";
        command.AddParam("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    /// <summary>将一行映射到 <see cref="Product"/>。时间戳以 ISO-8601 文本读取后解析。</summary>
    private static Product Map(DbDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Name = reader.GetString(1),
        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
        Price = reader.GetInt64(3) / 100m,
        Stock = reader.GetInt32(4),
        CreatedAt = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        UpdatedAt = DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
    };
}

