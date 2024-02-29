select
    c.nome,
    c.limite,
    c.saldo,
    sum
    (
        case when t.tipo = 1 then t.valor
        else t.valor * -1 end
    ) as soma_trasacoes
from
    public.clientes c
        join
    transacoes t on t.cliente_id = c.id
group by
    c.nome, c.limite , c.saldo 