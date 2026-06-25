using System.Data.Common;

namespace TursoDbDemo.Data;

/// <summary>ADO.NET <see cref="DbCommand"/> 扩展方法。</summary>
internal static class DbCommandExtensions
{
    /// <summary>添加带 <c>@</c> 前缀的参数并返回命令本身，支持链式调用。Nelknet 要求参数名带 <c>@</c> 前缀。</summary>
    internal static DbCommand AddParam(this DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
        return command;
    }
}
