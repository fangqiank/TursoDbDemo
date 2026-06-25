namespace TursoDbDemo.Models;

/// <summary>
/// 商品实体。对应 libSQL/Turso 中的 products 表。
/// </summary>
public class Product
{
    /// <summary>主键，自增。libSQL INTEGER PRIMARY KEY 对应 long。</summary>
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>价格，对应 libSQL REAL 列。</summary>
    public decimal Price { get; set; }

    public int Stock { get; set; }

    /// <summary>创建时间（UTC），以 ISO-8601 文本存储。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间（UTC），以 ISO-8601 文本存储。</summary>
    public DateTime UpdatedAt { get; set; }
}
