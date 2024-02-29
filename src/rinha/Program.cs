using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Rinha2024Q1;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Db")!, 
    dataSourceBuilderAction: a =>
    {
        a.UseLoggerFactory(NullLoggerFactory.Instance);
    });

builder.Services.AddSingleton<DbService>();

var app = builder.Build();

app.MapGet("clientes/{id:int}/extrato",
    async ValueTask<IResult> (int id,  DbService dbService, CancellationToken ct) =>
    {
        var extrato = await dbService.ObterExtratoAsync(id, ct);
        if (string.IsNullOrWhiteSpace(extrato))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Content(extrato, MediaTypeNames.Application.Json, Encoding.UTF8);
    });

app.MapPost("clientes/{id:int}/transacoes",
    async ValueTask<IResult> (int id, ModelState<CriaTransacao> modelState, DbService dbService,
        CancellationToken ct) =>
    {
        if (!modelState.IsValid || modelState.Model is null || !modelState.Model!.EhValida())
        {
            return TypedResults.UnprocessableEntity();
        }
        
        var resultadoTransacao = await dbService.CriarTransacaoAsync(id, modelState.Model, ct);
        if (resultadoTransacao is null)
        {
            return TypedResults.UnprocessableEntity();
        }
        if (resultadoTransacao.ClienteId == 0)
        {
            return TypedResults.NotFound();
        }
        if (!resultadoTransacao.TransacaoFoiCriada)
        {
            return TypedResults.UnprocessableEntity();
        }
        return TypedResults.Ok(new TransacaoCriada(resultadoTransacao.Limite, resultadoTransacao.Saldo));
    });

app.Run();