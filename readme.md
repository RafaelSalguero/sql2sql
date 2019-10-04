# Sql2Sql - A typed SQL-based micro ORM for PostgreSQL
[![Build Status](https://travis-ci.org/RafaelSalguero/sql2sql.png?branch=master)](https://travis-ci.org/RafaelSalguero/sql2sql)

The best of both worlds, use the full power of PostgreSQL without loosing type cheking.

Fully compatible with EF6 and EFCore!

The syntax is 99% pure SQL and directly based on the official PostgreSQL documentation.
If you know SQL you already know Sql2Sql!

```c#
//Plain/undecorated POCOs
class Animal 
{
    public string Type { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

using (var c = new MyContext()) {
    //Can be mixed with EF DbContext!
    
    //Fully typed SQL syntax including window functions, recursive queries, conditional aggregates and many more!
    var type = "dog"; //Parametize a query just by using variables inside of it
    var dogs = await Sql.From<Animal>()
        .Select(x => new {
            name = x.Name,
            age = x.Age
        })
        .Where(x => x.Type == type)
        //.ToString()    //Convert the query to SQL
        //.ToSql()       //Extract both SQL and parameters
        .ToListAsync(c); //Execute the query
}
```

## Why Sql2Sql?
Sql2Sql solves the ORM problem differently, instead of writing a complicated algorithm for translating LINQ to SQL *(EF way)* or giving up type checking and just using SQL strings *(Dapper way)* we made a C# fluent API that mimics the SQL language as-is.

Sql2Sql allow us to make strongly typed queries that use the full syntax of SQL.

Compared to EF, the performance of Sql2Sql queries is much more predictable since the generated SQL is obvious to the developer.

## Comparission 
.       |Typed                     | Predictible SQL     | Full-SQL           | Composable  
--------|--------------------------|---------------------|--------------------|---------------------
Sql2Sql |   :white_check_mark:     |  :white_check_mark: | :white_check_mark: | :white_check_mark:
EF      |   :white_check_mark:     |  :x:                | :x:                | :white_check_mark:
Dapper  |   :x:                    |  :white_check_mark: | :white_check_mark: | :white_check_mark:

- *\*Dapper can work with any database but if the database change the SQL code also needs to change*

## Why not database-agnostic?
Sql2Sql is not designed to be database-agnostic. SQL have many flavours, each with its own syntax. Sql2Sql strives to represent faithfully this unique syntax in order to give the programmer a minimal abstraction layer over the database without loosing type checking.

## Some supported goodies
- `WINDOW functions`
- `JOIN` clauses with arbitrary conditions, including the powerful `LATERAL JOIN`
- `DISTINCT ON` queries
- Value and query based `INSERT`
- Atomic *upserts* with `INSERT ... ON CONFLICT DO UPDATE`
- Conditional aggregates `agg(expr) WHERE cond` 
- Recursive queries `WITH RECURSIVE`
- Common table expressions
- Mix raw and typed queries
- Query composition
- Mapping to immutable types and EF complex types
- A wide range of native PostgreSQL functions

## Quick examples
### Execute a query

```csharp
var customers = await Sql
    .From<Customer>()
    .Select(x => x)
    .ToListAsync(connection) //'connection' is a DbContext or a NpgsqlConnection, depending on the package
    ;
```

### Parametized queries

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
```

### Joins

```csharp
var query = Sql
    .From<Customer>()
    .Join<Location>().On(x => x.Item1.LocationId = x.Item2.Id)
    .Alias(x => new {
        cust = x.Item1,
        loc = x.Item2
    })
    .Select(x => new {
        Name = x.cust.Name,
        Location = x.loc.Name
    })
    ;
```

## Almost ready
- `UPDATE` and `DELETE` clauses
- Set operations with `UNION`, `INTERSECT`, `EXCLUDE` 
- Hi performance *Il-emmited* based mapping

## Future
- Advanced grouping clauses: `CUBE`, `ROLLUP`, `GROUPING SETS`
- Mapping to `dynamic` objects
- Postgre *No-SQL* features, `JSON/SQL` support
- Syntax for other SQL flavors (MySql and SqlServer are planned)