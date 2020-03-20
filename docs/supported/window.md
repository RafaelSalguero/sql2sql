# `WINDOW` supported syntax

All `WINDOW` syntax is supported except `ORDER BY <expression> USING <operator>`

Example:
```csharp
Sql.From<Cliente>()
.Window(win => new
{
    //WINDOW definitions:
    w1 = win.Rows().UnboundedPreceding().AndCurrentRow(),
    w2 = win.OrderBy(x => x.Nombre)
})
.Select((x, win) => new //2nd argument = defined windows
{
    nom = x.Nombre,
    ids = Sql.Over(Sql.Sum(x.Nombre), win.w1) //Aggregate functions can reference defined windows 
});
```

## Definition syntax:

- `win` Window method argument, is used to create windows
    - Can be followed by any of:
        - `PartitonBy(x => expr)[.ThenBy(...)]`
            - Optional, `PARTITION BY` clause

        - `OrderBy(x => expr, order?, nulls?)[.ThenBy(...)]`
            - Optional, `ORDER BY` clause
            - `order` indicates `ASC` or `DESC`
            -  `nulls` indicates `NULLS FIRST` or `NULLS LAST`

    -  `Range()`, `Rows()`, `Groups()`
        - Indicates the window `FRAME` clause / frame grouping mode
        - Must be followed by:
            - **Frame start**, required, can be:
                - `UnboundedPreceding()`
                - `Preceding(offset)`
                - `CurrentRow()`
                - `Following(offset)`
                - `UnboundedFollowing()`
            - **Frame end**, required, can be:
                - `AndUnboundedPreceding()`
                - `AndPreceding(offset)`
                - `AndCurrentRow()`
                - `AndFollowing(offset)`
                - `AndUnboundedFollowing()`
            - **Frame exclusion**, optional can be:
                - `ExcludeCurrentRow()`
                - `ExcludeGroup()`
                - `ExcludeTies()`
                - `ExcludeNoOthers()`

## Reusing existing windows
Multiple `.Window()` calls are allowed, the 2nd and above calls can access the windows defined on the previous call, enabling window reusing.

Example:
```csharp
Sql.From<Cliente>()
.Window(win => new
{
    //Per PostgreSQL specification, only windows without a FRAME clause
    //can be reused but this is not imposed by Sql2Sql
    win1 = win.PartitionBy(x => x.IdState)
})
.Window((win, old) => new
{
    //Optionally include reused windows in order to use the
    //dedicated window reuse SQL syntax, if not included,
    //all the reused definition clauses will be copied to 
    //the defined window, functionally both options are equivalent
    old.win1,
    // Reference old windows to use as existing:
    win2 = old.win1.Rows().CurrentRow().AndUnboundedFollowing()

})
//Both win1 and win2 avalidable on SELECT expression
Select((x, win) => Sql.Over(Sql.Sum(x.Amount), win.win2)) 
```