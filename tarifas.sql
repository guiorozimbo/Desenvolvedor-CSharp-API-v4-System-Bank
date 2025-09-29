CREATE TABLE tarifa (
	idtarifa TEXT(37) PRIMARY KEY, -- identificacao unica da tarifa
	idcontacorrente TEXT(37) NOT NULL, -- identificacao unica da conta corrente
	datamovimento TEXT(25) NOT NULL, -- data do transferencia no formato DD/MM/YYYY
	valor REAL NOT NULL, -- valor da tarifa. Usar duas casas decimais.
	FOREIGN KEY(idtarifa) REFERENCES tarifa(idtarifa)
);