# `WINDOW` supported syntax

Example:
```csharp
Sql.From<Cliente>()
.Window(win => new
{
    //WINDOW definitions:
    w1 = win.Rows().UnboundedPreceding().AndCurrentRow(),
})
.Select((x, win) => new //2nd argument = Defined windows
{
    nom = x.Nombre,
    ids = Sql.Over(Sql.Sum(x.Nombre), win.w1)
});
```