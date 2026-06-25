using System.ComponentModel.DataAnnotations;

namespace TursoDbDemo.Models;

/// <summary>创建商品的请求模型。</summary>
public class CreateProductDto
{
    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(200, ErrorMessage = "名称最长 200 个字符")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "描述最长 2000 个字符")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "价格不能为空")]
    [Range(0, double.MaxValue, ErrorMessage = "价格必须大于等于 0")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "库存必须大于等于 0")]
    public int Stock { get; set; }
}

/// <summary>更新商品的请求模型。</summary>
public class UpdateProductDto
{
    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(200, ErrorMessage = "名称最长 200 个字符")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "描述最长 2000 个字符")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "价格不能为空")]
    [Range(0, double.MaxValue, ErrorMessage = "价格必须大于等于 0")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "库存必须大于等于 0")]
    public int Stock { get; set; }
}
