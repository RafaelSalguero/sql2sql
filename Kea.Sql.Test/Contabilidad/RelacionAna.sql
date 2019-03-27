--Encuentra los saldos de las cuentas detalle de interes:
WITH RECURSIVE detalle AS (
	SELECT 
		c."IdRaiz", c."IdRegistro" AS "IdCuenta", c."IdCuentaPadre", c."Terminacion", c."Nombre",
		movs."CargoAnt", movs."AbonoAnt", movs."CargoPer", movs."AbonoPer"
	FROM 
	(
		/*
		Por cada cuenta, encuentra todas las cuentas de detalle que se tienen que sumar para encontrar su saldo:
		*/
		WITH RECURSIVE cuentas AS (
			--Cuentas de las que nos interesa la relación analítica:
			SELECT * FROM
			(
			
				SELECT 
					"IdRegistro" AS "IdRaiz", 
					"IdRegistro", 
					"Terminacion", 
					"Nombre", 
					"IdCuentaPadre", 
					CASE WHEN "IdCuentaPadre" IS NULL THEN 0 ELSE 1 END AS "Tipo" 
					FROM "CuentaAcumulativa" 
				UNION ALL 
				SELECT 
					"IdRegistro" AS "IdRaiz",
					"IdRegistro", 
					"Terminacion", 
					"Nombre", 
					"IdCuentaPadre", 
					2 AS "Tipo" 
					FROM "CuentaDetalle" 
			) todas

			--FILTRO: Las cuentas de interes
			WHERE todas."IdRegistro" =  '02bcd575-75ec-48bb-af43-c517fe65af4f'

			--Obtener todas las subcuentas hijas de esas cuentas de interes, note que aquí estan revueltas las acumulativas como las de detalle
			UNION ALL
			SELECT cuentas."IdRaiz", ac."IdRegistro", ac."Terminacion", ac."Nombre", ac."IdCuentaPadre", ac."Tipo" FROM 
			(
				SELECT "IdRegistro", "Terminacion", "Nombre", "IdCuentaPadre", 1 AS "Tipo" FROM "CuentaAcumulativa" 
				UNION ALL 
				SELECT "IdRegistro", "Terminacion", "Nombre", "IdCuentaPadre", 2 AS "Tipo" FROM "CuentaDetalle" 
			)  ac, cuentas WHERE ac."IdCuentaPadre" = cuentas."IdRegistro"
			
		)
		SELECT * FROM cuentas
		--Sólo nos interesan las de detalle:
		WHERE "Tipo" = 2
		--El ORDER BY es sólo por conveniencia para depurar
		ORDER BY "Terminacion"
	) c

	LEFT JOIN LATERAL (
		SELECT 
			--FILTRO: Rango de fechas de interes
			coalesce(sum("Cargo") FILTER (WHERE "Fecha" BETWEEN '2018-12-01' AND '2018-12-31'), 0) AS "CargoPer", 
			coalesce(sum("Abono") FILTER (WHERE "Fecha" BETWEEN '2018-12-01' AND '2018-12-31'), 0) AS "AbonoPer",
			coalesce(sum("Cargo") FILTER (WHERE "Fecha" < '2018-12-01'), 0) AS "CargoAnt", 
			coalesce(sum("Abono") FILTER (WHERE "Fecha" < '2018-12-01'), 0) AS "AbonoAnt"
		FROM
		(
			SELECT 
				CASE mov."TipoMovimiento" WHEN 0 THEN mov."Importe" ELSE 0 END AS "Cargo",
				CASE mov."TipoMovimiento" WHEN 1 THEN mov."Importe" ELSE 0 END AS "Abono",
				pol."Fecha"
			FROM "Movimiento" mov 
			JOIN "Poliza" pol ON pol."IdRegistro" = mov."IdPoliza"
			WHERE 
				mov."IdCuentaDetalle" = c."IdRegistro" AND
				pol."Aplicada" AND NOT pol."Borrada"
		) q
	) movs ON true
),  rec AS (
	--Obtener los acumulados de todos los niveles hacia arriba del detalle:
	SELECT * FROM detalle
	UNION ALL
	
	--Buscamos todos los padres directos de las cuentas:
	SELECT 
		d."IdRaiz", d."IdCuentaPadre" AS "IdCuenta", c."IdCuentaPadre", c."Terminacion", c."Nombre", 
		d."CargoAnt", d."AbonoAnt", d."CargoPer", d."AbonoPer"
	FROM rec d
	JOIN "CuentaAcumulativa" c ON c."IdRegistro" = d."IdCuentaPadre"
	
)
--Sumar por cuenta, esto es porque las cuentas acumulativas van a tener varios registros en el "rec"
SELECT 
	
	d."IdRaiz", d."IdCuentaPadre" AS "IdCuenta", d."IdCuentaPadre", d."Terminacion", d."Nombre",
	sum(d."CargoAnt") AS "CargoAnt", sum(d."AbonoAnt") AS "AbonoAnt", sum(d."CargoPer") AS "CargoPer", sum(d."AbonoPer") AS "AbonoPer"
FROM rec d
GROUP BY d."IdRaiz", d."IdCuenta" , d."IdCuentaPadre", d."Terminacion", d."Nombre"