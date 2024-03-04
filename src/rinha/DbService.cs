using System.Runtime.CompilerServices;
using Npgsql;

namespace Rinha2024Q1;

public sealed class DbService
{
    private readonly NpgsqlDataSource _npgsqlDataSource;

    public DbService(NpgsqlDataSource npgsqlDataSource)
    {
        _npgsqlDataSource = npgsqlDataSource;
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<ResultadoTransacao?> CriarTransacaoAsync(int clienteId, CriaTransacao criaTransacao, 
        CancellationToken ct)
    {
        ResultadoTransacao? resultadoTransacao = null;
        var connection = await _npgsqlDataSource.OpenConnectionAsync(ct);
        var command = connection.CreateCommand();
        command.Parameters.Add(new NpgsqlParameter("clienteId", clienteId));
        command.Parameters.Add(new NpgsqlParameter("valor", criaTransacao.Valor));
        command.Parameters.Add(new NpgsqlParameter("tipo", (int)criaTransacao.Tipo));
        command.Parameters.Add(new NpgsqlParameter("descricao", criaTransacao.Descricao));
        command.CommandText = CriaTransacaoQuery;
        await using var reader = await command.ExecuteReaderAsync(ct);
        
        while (await reader.ReadAsync(ct))
        {
            resultadoTransacao = new ResultadoTransacao(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetBoolean(3)
            );
        }
        await reader.CloseAsync();
        
        return resultadoTransacao;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<string?> ObterExtratoAsync(int clienteId, CancellationToken ct)
    {
        var connection = await _npgsqlDataSource.OpenConnectionAsync(ct);
        var command = connection.CreateCommand();
        command.Parameters.Add(new NpgsqlParameter("clienteId", clienteId));
        command.CommandText = ObterExtratoQuery;
        await using var reader = await command.ExecuteReaderAsync(ct);
        string? extrato = null;
        while (await reader.ReadAsync(ct))
        {
            extrato = reader.GetString(1);
        }
        await reader.CloseAsync();
        return extrato;
    }
   
    private const string ObterExtratoQuery =
        """
         refresh materialized view concurrently vw_extrato;
         select
             *
         from
         	public.vw_extrato c
         where
             c.id = @clienteId;
         """;

    private const string CriaTransacaoQuery =
        """
         call public.criar_transacao
         (
             @clienteId,
             @valor,
             @tipo,
             @descricao
         );
         """;
}