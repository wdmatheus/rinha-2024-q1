using System.Text.Json.Serialization;

namespace Rinha2024Q1;

// ReSharper disable all InconsistentNaming
// ReSharper disable all RedundantTypeDeclarationBody

readonly record struct ModelState<T>(T? Model, Exception? Exception)
{
    public bool IsValid => Exception is null;

    public static async ValueTask<ModelState<T>> BindAsync(HttpContext httpContext)
    {
        try
        {
            var item = await httpContext.Request.ReadFromJsonAsync<T>();

            return new(item, null);
        }
        catch (Exception ex)
        {
            return new(default, ex);
        }
    }
}

[JsonConverter(typeof(JsonStringEnumConverter<ETipoTransacao>))]
public enum ETipoTransacao
{
    c = 1,
    d = 2
}

public sealed record CriaTransacao(int Valor, ETipoTransacao Tipo, string Descricao)
{
    public bool EhValida() =>
        Valor > 0 &&
        !string.IsNullOrWhiteSpace(Descricao) &&
        Descricao.Length <= 10;
}

public sealed record TransacaoCriada(int Limite, int Saldo);

public sealed record ResultadoTransacao(int ClienteId, int Limite, int Saldo, bool TransacaoFoiCriada);

[JsonSerializable(typeof(CriaTransacao))]
[JsonSerializable(typeof(TransacaoCriada))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(DateTime))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}