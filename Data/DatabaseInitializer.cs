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
                    price_cents INTEGER NOT NULL,
                    stock       INTEGER NOT NULL DEFAULT 0,
                    created_at  TEXT NOT NULL,
                    updated_at  TEXT NOT NULL
                );
                """;
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
        }
       logger.LogInformation("products 表已就绪");

        // Schema 迁移：检查旧版 price 列是否存在（price_cents 之前的 schema）
        await using (var checkCol = connection.CreateCommand())
        {
            checkCol.CommandText = "SELECT COUNT(*) FROM pragma_table_info('products') WHERE name = 'price';";
            var hasPrice = Convert.ToInt64(await checkCol.ExecuteScalarAsync(cancellationToken)) > 0;

            if (hasPrice)
            {
                logger.LogInformation("检测到旧版 schema（price 列），正在迁移至 price_cents…");

                await using (var addCol = connection.CreateCommand())
                {
                    addCol.CommandText = "ALTER TABLE products ADD COLUMN price_cents INTEGER NOT NULL DEFAULT 0;";
                    await addCol.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var updateCmd = connection.CreateCommand())
                {
                    updateCmd.CommandText = "UPDATE products SET price_cents = CAST(ROUND(price * 100) AS INTEGER);";
                    await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var dropCol = connection.CreateCommand())
                {
                    dropCol.CommandText = "ALTER TABLE products DROP COLUMN price;";
                    await dropCol.ExecuteNonQueryAsync(cancellationToken);
                }

                logger.LogInformation("schema 迁移完成");
            }
        }

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
            INSERT INTO products (name, description, price_cents, stock, created_at, updated_at)
            VALUES
                (@Name1, @Description1, @Price1, @Stock1, @Now, @Now),
                (@Name2, @Description2, @Price2, @Stock2, @Now, @Now),
                (@Name3, @Description3, @Price3, @Stock3, @Now, @Now);
            """;
        command.AddParam("@Name1", "笔记本电脑");
        command.AddParam("@Description1", "16GB 内存，512GB SSD");
        command.AddParam("@Price1", 699900);
        command.AddParam("@Stock1", 50);
        command.AddParam("@Name2", "无线鼠标");
        command.AddParam("@Description2", "静音点击，蓝牙双模");
        command.AddParam("@Price2", 12900);
        command.AddParam("@Stock2", 200);
        command.AddParam("@Name3", "机械键盘");
        command.AddParam("@Description3", "茶轴，RGB 背光");
        command.AddParam("@Price3", 39900);
        command.AddParam("@Stock3", 100);
        command.AddParam("@Now", now);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

}
