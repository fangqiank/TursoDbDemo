using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using TursoDbDemo.Models;
using TursoDbDemo.Services;

namespace TursoDbDemo.Endpoints;

/// <summary>商品 CRUD 端点注册。</summary>
public static class ProductEndpoints
{
    /// <summary>将商品相关端点映射到 <c>/api/products</c> 路由组。</summary>
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("", GetAll)
            .WithName("GetAllProducts")
            .WithSummary("获取全部商品");

        group.MapGet("/{id:long}", GetById)
            .WithName("GetProductById")
            .WithSummary("按 ID 获取商品");

        group.MapPost("", Create)
            .WithName("CreateProduct")
            .WithSummary("创建商品");

        group.MapPut("/{id:long}", Update)
            .WithName("UpdateProduct")
            .WithSummary("更新商品");

        group.MapDelete("/{id:long}", Delete)
            .WithName("DeleteProduct")
            .WithSummary("删除商品");

        return app;
    }

    private static async Task<Ok<IEnumerable<Product>>> GetAll(
        IProductService service, CancellationToken ct)
        => TypedResults.Ok(await service.GetAllAsync(ct));

    private static async Task<Results<Ok<Product>, NotFound>> GetById(
        long id, IProductService service, CancellationToken ct)
    {
        var product = await service.GetByIdAsync(id, ct);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    private static async Task<Results<Created<Product>, ValidationProblem>> Create(
        CreateProductDto dto, IProductService service, CancellationToken ct)
    {
        if (TryValidate(dto, out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        var product = await service.CreateAsync(dto, ct);
        return TypedResults.Created($"/api/products/{product.Id}", product);
    }

    private static async Task<Results<Ok<Product>, NotFound, ValidationProblem>> Update(
        long id, UpdateProductDto dto, IProductService service, CancellationToken ct)
    {
        if (TryValidate(dto, out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        var product = await service.UpdateAsync(id, dto, ct);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        long id, IProductService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    /// <summary>基于 DataAnnotations 校验 DTO。返回 true 表示校验失败（并填充 errors）。</summary>
    private static bool TryValidate(object dto, out IDictionary<string, string[]> errors)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        if (isValid)
        {
            errors = new Dictionary<string, string[]>();
            return false;
        }

        errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => (Member: member, Error: r.ErrorMessage ?? "无效")))
            .GroupBy(x => x.Member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Error).Distinct().ToArray());
        return true;
    }
}
