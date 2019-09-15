--SELECT * FROM "CuentaAcumulativa"  WHERE "IdCuentaPadre"  IS NULL
WITH RECURSIVE cuentasInfo AS (
	--Información individual de las cuentas
	WITH RECURSIVE cuentas AS (
		--Query de ejemplo simplemente, todos los hijos de la cuenta mayor 8, este query se va a sustituir por el query que tenga las cuentas correspondientes
		SELECT 
			null::uuid AS "IdCuentaDet", 
			"IdRegistro" AS "IdCuentaAcum"
		FROM "CuentaAcumulativa" WHERE "IdCuentaPadre" IS NULL

		UNION ALL

		--Todos los hijos de estas cuentas:
		SELECT hijo."IdCuentaDet", hijo."IdCuentaAcum" FROM (
			SELECT 
				null::uuid AS "IdCuentaDet", 
				"IdRegistro" AS "IdCuentaAcum",
				"IdCuentaPadre"
			FROM "CuentaAcumulativa" ac

			UNION ALL

			SELECT 
				"IdRegistro" AS "IdCuentaDet",
				null::uuid AS "IdCuentaAcum",
				"IdCuentaPadre"
			FROM "CuentaDetalle" det
		) hijo, cuentas padre WHERE hijo."IdCuentaPadre" = padre."IdCuentaAcum"
	)
	SELECT 
		cu.*, 
		coalesce(det."IdCuentaPadre", acum."IdCuentaPadre") AS "IdCuentaPadre",
		coalesce(det."Nombre", acum."Nombre") AS "Nombre",
		coalesce(det."Terminacion", acum."Terminacion") AS "Terminacion",
		CASE WHEN coalesce(det."IdCuentaPadre", acum."IdCuentaPadre") IS NULL THEN "IdCuentaAcum" ELSE null::uuid END AS "IdCuentaMayor"
	FROM cuentas cu
	LEFT JOIN "CuentaDetalle" det ON det."IdRegistro" = cu."IdCuentaDet"
	LEFT JOIN "CuentaAcumulativa" acum ON acum."IdRegistro" = cu."IdCuentaAcum"
	
), rec AS (
	SELECT *, "IdCuentaPadre" AS "IdCuentaSup", ARRAY[c."Terminacion"] AS "Num" FROM cuentasInfo c

	UNION ALL

	--Buscar los padres de las cuentas:
	SELECT 
		c."IdCuentaDet" AS "IdCuentaDet", 
		c."IdCuentaAcum" AS "IdCuentaAcum",
		c."IdCuentaPadre",
		c."Nombre",
		c."Terminacion",
		CASE WHEN acum."IdCuentaPadre" IS NULL THEN acum."IdRegistro" ELSE c."IdCuentaMayor" END AS "IdCuentaMayor",
		acum."IdCuentaPadre" AS "IdCuentaSup",
		acum."Terminacion" || c."Num"
		
	FROM rec c
	JOIN "CuentaAcumulativa" acum ON acum."IdRegistro" = c."IdCuentaSup"
	
)
SELECT 
	r."IdCuentaDet",
	r."IdCuentaAcum",
	r."IdCuentaPadre",
	r."Nombre",
	r."Terminacion",
	r."IdCuentaMayor",
	array_to_string(r."Num", '-') || '-' AS "Numero"
FROM rec r
WHERE "IdCuentaMayor" IS NOT NULL