## `FROM list`
Aqui se definen las tablas, su estructura es equivalente a la sintaxis de postgres `FROM from_item [, ....]`

Ejemplos:

**Una tabla**
```c#
//SELECT * FROM "Factura"
Sql
.FromTable<Factura>()
.Select(x => x)
;
```

**Un LEFT JOIN**
```c#
Sql
.FromTable<Factura>()
//LEFT JOIN a la tabla "Cliente"
.Left().JoinTable<Cliente>()
.OnMap((fac,cli) => new {
    //Alias del lado izquierdo y derecho del JOIN:
    fac = fac,
    cli = cli
}, x => x.fac.IdCliente == x.cli.IdRegistro) //Condición del JOIN
```

**Múltiples INNER JOIN**
```c#
Sqlr
.FromTable<Factura>()
//JOIN a la tabla "Cliente", sin alias, "Factura" queda como Item1 y "Cliente" como Item2
.Inner().JoinTable<Cliente>().OnTuple(x => x.Item1.IdCliente == x.Item2.IdRegistro)
//JOIN entre la tabla "Factura" y "FormaPago"
.Inner().JoinTable<FormaPago>().On(x => x.Item1.IdFormaPago == x.Item3.IdRegistro)
//JOIN entre "Cliente" y "Grupo"
.Inner().JoinTable<Grupo>().On(x => x.Item2.IdGrupo == x.Item4.IdRegistro)
//Darle un nombre más descriptido a los elementos del JOIN:
.Alias(x => new 
{
    fac = x.Item1,
    cli = x.Item2,
    fpa = x.Item3,
    gru = x.Item4
})
.Select(x => new {
    folio = x.fac.Folio,
    cliente = x.cli.Nombre,
    formaPago = x.fpa.Descripcion,
    grupoCliente = x.gru.Nombre
})

/*
SELECT 
    fac."Folio" AS folio, 
    cli."Nombre" AS cliente, 
    fpa."Descripcion" AS formaPago, 
    gru."Nombre" AS grupoCliente
FROM "Factura" fac
INNER JOIN "Cliente" cli ON fac."IdCliente" = cli."IdRegistro"
INNER JOIN "FormaPago" fpa ON fac."IdFormaPago" = fpa."IdRegistro"
INNER JOIN "Grupo" gru ON cli."IdGrupo" = gru."IdRegistro"
*/
```

**LATERAL JOIN**
```c#
Sql
.FromTable<Factura>()
//LATERAL JOIN
.Left().Lateral(fac => 
    //Subquery en función del lado izquierdo del LATERAL JOIN, en este caso la factura
    Sql
    .FromTable<Concepto>()
    .Select(x => new {
        Iva = Sql.Sum(x.Iva),
        Subtotal = Sql.Sum(x.Precio * x.Cantidad)
    })
    .Where(x => x.IdFactura = fac.IdRegistro)
)
.OnTuple(x => true) //Condición del JOIN LATERAL
.Alias(x => new
{
    fac = x.Item1,
    con = x.Item2
})
.Select(x => new
{
    IdFactura = x.fac.IdRegistro,
    Iva = x.con.Iva,
    Subtotal = x.con.Subtotal
});
```