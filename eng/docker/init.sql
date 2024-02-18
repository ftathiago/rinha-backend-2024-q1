ALTER SYSTEM SET max_connections TO '2000';

CREATE UNLOGGED TABLE clientes (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(50) NOT NULL,
    limite INTEGER NOT NULL,
    saldo_atual integer not null default 0,
    versao integer not null default 0
);

CREATE UNLOGGED TABLE transacoes (
    id SERIAL PRIMARY KEY,
    cliente_id INTEGER NOT NULL,
    valor INTEGER NOT NULL,
    tipo CHAR(1) NOT NULL,
    descricao VARCHAR(10) NOT NULL,
    realizada_em TIMESTAMP NOT NULL DEFAULT NOW(),
    versao integer not null,
    CONSTRAINT fk_clientes_transacoes_id
        FOREIGN KEY (cliente_id) REFERENCES clientes(id)
);

CREATE UNIQUE INDEX uk_clientes_transacoes_versao ON transacoes (cliente_id,versao);

DO $$
BEGIN
    INSERT INTO clientes (nome, limite)
    VALUES
        ('o barato sai caro', 1000 * 100),
        ('zan corp ltda', 800 * 100),
        ('les cruders', 10000 * 100),
        ('padaria joia de cocaia', 100000 * 100),
        ('kid mais', 5000 * 100);    

END;
$$;

CREATE OR REPLACE FUNCTION public.efetuar_transacao(descricao character varying, valor integer, tipo character, client_id integer, OUT operation_status integer, OUT out_saldo_atual integer, OUT out_limite integer)
 RETURNS record
 LANGUAGE plpgsql
AS $function$
declare versao_antiga int;	
	    novo_saldo int;
	    atualizados int;
begin
--  begin
		/*
		 * Operation status
		 * 1= Sucesso
		 * 2= Saldo Insuficiente
		 * 3= Conflito
		 */	
	  select saldo_atual 
	       , versao 
	       , limite
	  from clientes
	  where id = client_id
	   into out_saldo_atual, 
	        versao_antiga, 
	        out_limite;	 

	       novo_saldo := out_saldo_atual - valor;
      if tipo = 'd' then
	    novo_saldo := out_saldo_atual + valor;
	  end if;
	  if (out_limite * -1) > novo_saldo then
	    operation_status := 2;
	    return;  
	  end if;
	  
	  update clientes c set 
	  	c.saldo_atual = novo_saldo,
	  	c.versao = versao_antiga + 1
	  where c.id = client_id
	    and c.versao = versao_antiga;

      GET DIAGNOSTICS atualizados = ROW_COUNT;   
	  if atualizados = 0 then
	  	operation_status := 3; 
		return;
	  end if;
	 
	  INSERT INTO transacoes (
	 	cliente_id, 
	 	valor, 
	 	tipo, 
		descricao, 
		realizada_em, 
		versao
	  ) VALUES(
	 	client_id, 
		valor, 
		tipo,
		descricao, 
	 	now(), 
	 	versao_antiga + 1);
--  exception when others then 
  	operation_status := 1;	
--  end;	
END;
$function$
;
