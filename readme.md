# Sql2Sql - A typed SQL-based micro ORM for PostgreSQL
The best of both worlds, use the full power of PostgreSQL without loosing type cheking.

Fully compatible with EF6 and EFCore!

The syntax is 99% pure SQL, if you know SQL you already know Sql2Sql!

```c#
using (var c = new MyContext()) {
    //Sql2Sql can be mixed with EF DbContext!
    
    /*Fully typed SQL syntax including window functions, recursive queries, conditional aggregates and many more!*/
    var type = "dog"; //Parameters!
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
.       |Typed      | Predictible SQL | Full-SQL | Composable  | Database-agnostic
--------|-----------|-----------------|----------|-------------|-------------------
Sql2Sql |   Yes     |  Yes            | Yes      | Yes         | No
EF      |   Yes     |  No             | No       | Yes         | Yes
Dapper  |   No      |  Yes            | Yes      | No          | No*

- *\*Dapper can work with any database but if the database change the SQL code also needs to change*

## Why not database-agnostic?
Sql2Sql is not designed to be database-agnostic, instead, each flavor (currently only PostgreSQL) has its own syntax, just as pure SQL.
This allows us to use the full capabilities of each SQL flavor and to focus development on other more useful features such as giving the user a powerful strongly typed query language.

In our experience, is very uncommon to switch databases mid-project.

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

## Almost ready
- `UPDATE` and `DELETE` clauses
- Set operations with `UNION`, `INTERSECT`, `EXCLUDE` 

## Future
- Advanced grouping clauses: `CUBE`, `ROLLUP`, `GROUPING SETS`
- Mapping to `dynamic` objects
- Postgre *No-SQL* features, `JSON/SQL` support
- Syntax for other SQL flavors (MySql and SqlServer are planned)