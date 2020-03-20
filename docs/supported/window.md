# `WINDOW` supported syntax

Example:
```csharp
Sql.From<Cliente>()
.Window(win => new
{
    //WINDOW definitions:
    w1 = win.Rows().UnboundedPreceding().AndCurrentRow(),
    w2 = win.OrderBy(x => x.Nombre)
})
.Select((x, win) => new //2nd argument = Defined windows
{
    nom = x.Nombre,
    ids = Sql.Over(Sql.Sum(x.Nombre), win.w1)
});
```

## Definition syntax:

- `win` Window method argument
    - Can be followed by any of:
        - `PartitonBy(x => expr)[.ThenBy(...)]`
            - Optional, `PARTITION BY` clause
            - `ThenBy(x => expr) ` Optional, extra `PARTITION BY` expressions

        - `OrderBy(x => expr, order?, nulls?)[.ThenBy(...)]`
            - Optional, `ORDER BY` clause
            - `order` indicates `ASC` or `DESC`
            -  `nulls` indicates `NULLS FIRST` or `NULLS LAST`

    -  `Range()`, `Rows()`, `Groups()`
        - Indicates the frame grouping mode
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