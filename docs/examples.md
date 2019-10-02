# Examples

## Execute a query (works for EF6, EFCore, and Npgsql packages)

```csharp
//
//'connection' is a DbContext or a NpgsqlConnection, depending on the package
var customers = await Sql
    .From(new SqlTable<Customer>())
    .Select(x => x)
    .ToListAsync(connection) //ToListAsync() method is available in the packages EF6, EFCore and Npgsql
    ;
```

## Simple parametized select

```csharp
var name = "Frida Kahlo";
var query = Sql
    .From<Customer>()
    .Select(x => new
    {
        nom = x.Name,
        loc = x.LocationId
    })
    .Where(x => x.Name == name)
    ;

//Inspect the generated SQL and parameter values before executing it:
var sql = query.ToSql();

//Execute the query:
var customers = await query.ToListAsync();
```

```SQL
SELECT 
    "Name" as nom, 
    "LocationId" as loc
FROM "Customer" x
WHERE x."Name" = @name
```

## Simple `JOIN`

```csharp
Sql
    .From<Customer>()
    .Inner().Join<Location>().On
    ;
```