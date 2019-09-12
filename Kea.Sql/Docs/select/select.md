# Select
Esta formado por las siguientes partes:

- `FROM list:` Se definen las tablas que van a participar, aquí esta el `FROM` inicial y los `JOIN`s
Opcionalmente se le puede dar un alias a las tablas del from list
- `DISTINCT` *(opcional)*
- `SELECT expression:` Expresa el resultado del `SELECT`, aqui se definen las columnas, puede devolver 1 valor singular
- `extensions:` *(opcional)* Todas las otras cláusulas tales como `WHERE`, `ORDER BY`, `GROUP BY`, etc...

**Ejemplo**
```c#
Sql
.FromTable<Factura>() //Este es el FROM list, sólo tiene una tabla 
.Select(x => x) //Esta es la SELECT expression, devolver el objeto completo representa un *
.Where(x => x.Folio = "1234") //Extensions, en este caso sólo un WHERE
;

/*
SELECT * 
FROM "Factura"
WHERE "Folio" = '1234'
*/
```