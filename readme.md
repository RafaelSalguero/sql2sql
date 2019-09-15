# Sql2Sql - A typed micro ORM for PostgreSQL
The best of both worlds, use the full power of PostgreSQL syntax without loosing type cheking.

Fully compatible with EF6 and EFCore.

The syntax is 99% pure SQL, if you know SQL you already know Sql2Sql!

```c#
using (var c = new MyContext()) {
    //Sql2Sql can be mixed with EF DbContext!
    
    /*Fully typed SQL syntax including window functions, recursive queries, conditional aggregates and many more!*/
        var type = "dog";
        var dogs = await Sql.FromTable<Animal>()
        .Select(x => new {
            name = x.Name,
            age = x.Age
        })
        .Where(x => x.Type == type)
        .ToListAsync(c);
}
```

## Why Sql2Sql?
Sql2Sql solves the ORM problem differently, instead of writing a complicated algorithm for translating LINQ to SQL *(EF way)* or giving up type checking and just using SQL strings *(Dapper way)* we made a C# fluent API that mimics the SQL language as-is.

Sql2Sql allow us to make strongly typed queries that use the full syntax of SQL.

Compared to EF, the performance of Sql2Sql queries is much more predictable since the generated SQL is obvious to the developer.

## Comparission 
.       |Typed      | Predictible SQL | Full-SQL | Composable
--------|-----------|-----------------|----------|-------------
Sql2Sql |   Yes     |  Yes            | Yes      | Yes
EF      |   Yes     |  No             | No       | Yes
Dapper  |   No      |  Yes            | Yes      | No

## Some supported goodies
- `WINDOW functions`
- `JOIN` clauses with arbitrary conditions, including the powerful `LATERAL JOIN`
- `DISTINCT ON` queries
- Query based `INSERT` and atomic *upserts* with `ON CONFLICT DO UPDATE`
- Conditiona aggregates `agg(expr) WHERE cond` 
- Recursive queries `WITH RECURSIVE`
- Common table expressions
- Mix raw and typed queries
- Query composition
- Mapping to immutable types and EF complex types
- A wide range of native PostgreSQL functions

## Almost ready
- `UPDATE` and `DELETE` clauses
- Set operations with `UNION`, `INTERSECT`, `EXCLUDE` 
- Advanced grouping clauses: `CUBE`, `ROLLUP`, `GROUPING SETS`

## Future
- Syntax for other SQL flavors (MySql and SqlServer are planned)
- Mapping to `dynamic` objects