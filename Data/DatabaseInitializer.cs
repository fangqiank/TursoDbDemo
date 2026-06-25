using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace TursoDbDemo.Data;

/// <summary>
/// 应用启动时初始化数据库：创建 products 表（如不存在）并写入种子数据（仅首次）。
/// </summary>
public class DatabaseInitializer(ILibSqlConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
{
    /// <summary>执行建表与种子数据写入。</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var connection = connectionFactory.Create();
        await connectionFactory.EnsureOpenAsync(cancellationToken);

        await using (var createCommand = connection.CreateCommand())
        {
            createCommand.CommandText = """
                CREATE TABLE IF NOT EXISTS products (
                    id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    name        TEXT NOT NULL,
                    description TEXT,
                    price       REAL NOT NULL,
                    stock       INTEGER NOT NULL DEFAULT 0,
                    created_at  TEXT NOT NULL,
                    updated_at  TEXT NOT NULL
                );
                """;
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        logger.LogInformation("products 表已就绪");

        long rowCount;
        await using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = "SELECT COUNT(*) FROM products;";
            rowCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));
        }

        if (rowCount == 0)
        {
            await SeedAsync(connection, cancellationToken);
            logger.LogInformation("已写入 {Count} 条种子数据", 3);
        }
    }

    private static async Task SeedAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow.ToString("O");
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO products (name, description, price, stock, created_at, updated_at)
            VALUES
                (@Name1, @Description1, @Price1, @Stock1, @Now, @Now),
                (@Name2, @Description2, @Price2, @Stock2, @Now, @Now),
                (@Name3, @Description3, @Price3, @Stock3, @Now, @Now);
            """;
        AddParameter(command, "@Name1", "笔记本电脑");
        AddParameter(command, "@Description1", "16GB 内存，512GB SSD");
        AddParameter(command, "@Price1", 6999.00m);
        AddParameter(command, "@Stock1", 50);
        AddParameter(command, "@Name2", "无线鼠标");
        AddParameter(command, "@Description2", "静音点击，蓝牙双模");
        AddParameter(command, "@Price2", 129.00m);
        AddParameter(command, "@Stock2", 200);
        AddParameter(command, "@Name3", "机械键盘");
        AddParameter(command, "@Description3", "茶轴，RGB 背光");
        AddParameter(command, "@Price3", 399.00m);
        AddParameter(command, "@Stock3", 100);
        AddParameter(command, "@Now", now);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>添加带 <c>@</c> 前缀的参数（Nelknet 要求参数名带前缀）。</summary>
    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
