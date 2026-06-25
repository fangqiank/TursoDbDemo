using TursoDbDemo.Models;

namespace TursoDbDemo.Services;

/// <summary>商品 CRUD 业务接口。</summary>
public interface IProductService
{
    /// <summary>获取全部商品。</summary>
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>按主键获取商品；不存在返回 null。</summary>
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>创建商品，返回持久化后的完整对象。</summary>
    Task<Product> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);

    /// <summary>更新商品；不存在返回 null，存在返回更新后的对象。</summary>
    Task<Product?> UpdateAsync(long id, UpdateProductDto dto, CancellationToken cancellationToken = default);

    /// <summary>删除商品；返回是否删除了记录。</summary>
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
