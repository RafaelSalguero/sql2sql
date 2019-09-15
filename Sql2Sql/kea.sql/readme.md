 # Kea.Sql

## Objetivo
Ser una pequeña capa de abstracción entre postgre SQL y C#, de tal manera que se pueda escribir casi
tal cual el código de SQL en C#, mezclando la facilidad de uso de un lenguaje tipado y el rendimiento
de SQL puro.

La librería esta diseñada específicamente para Postgres, así que todas las características y
extensiones extras que ofrece Postgres están disponibles

## Comparación con otras tecnologías
- **Entity Framework:** Tiene su propio lenguaje tipado (linq), pero este sólo provee las 
funciones básicas de SQL. Para queries más complicados, resulta muy dificil predecir el comportamiento que va a tener
en producción, ya que el SQL generado es muy diferente al lenguaje original, en estos casos el SQL generado es 
normalmente muy ineficiente.

- **Dapper:** No tiene un lenguaje tipado, funciona por medio de `string`s de SQL, por lo que el compilador
no avisa en caso de errores en el SQL. Su función es sólo la de mapear los resultados de queries a objetos.

.       |Tipado    | SQL predecible  | Full-SQL | Composición 
--------|----------|-----------------|----------|-------------
EF      |   Si     |  No             | No       | Si
Dapper  |   No     |  Si             | Si       | No
Kea.Sql |   Si     |  Si             | Si       | Si

## Soportado por `Kea.Sql` pero no por `Entity Framework`
### `SELECT`
- `JOIN` con condiciones arbitrárias, no sólo con igualdades
- `WINDOW functions`, funciones de agregado acumulativo
- Usar funciones de agregado condicional `agg(expr) WHERE cond` 
- Queries recursivos con `WITH RECURSIVE`
- Common table expressions con `WITH`
- `LATERAL JOIN`
- Mapeo a tipos inmutables (no sólo a tipos mutables)
- Funciones nativas de postgres (manejo de cadenas, fechas, matemáticas, etc...)

### `INSERT`
- `ON CONFLICT DO UPDATE` operaciones de insert/update concurrentes
- `RETURNING` query en función de filas insertadass

## Se soportará en un futuro:
- `UPDATE`
- Lógica de conjuntos con `UNION`, `INTERSECT`, `EXCLUDE`
- Saltar filas con `OFFSET`
- Funciones de cubos `GROUP BY CUBE`, `ROLLUP`, `GROUPING SETS`