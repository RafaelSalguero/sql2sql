# `SELECT` supported syntax
> Check also the [expressions supported syntax](./expression.md), 
> used heavily in this examples

-  :white_check_mark: `WITH` 
    - Details in [WITH supported syntax](./with.md)
    - :white_check_mark: `WITH`
    - :white_check_mark: `WITH RECURSIVE`

-  :white_check_mark: Select all from table
    ```csharp
    Sql.From<Customer>();
    //or
    Sql.From<Customer>().Select(x => x);
    ```

-  :white_check_mark: `DISTINCT`
    ```csharp
    Sql
    .From<Customer>()
    .Distinct()
    ```

-  :white_check_mark: `DISTINCT ON (expr)`
    ```csharp
    Sql
    .From<Customer>()
    .DistinctOn(x => x.LocationId)
    ```

-  :white_check_mark: `SELECT *, ...`
    ```csharp
    Sql
    .From<Customer>()
    .Select(x => Sql.Star().Map(new
    {
        FullName = x.Name + x.LastName
    }));
    ```

-  :full_moon: `FROM`
    - Details in [FROM supported syntax](./from.md)
    - :white_check_mark: `FROM` table
    - :white_check_mark: `FROM` Subquery
    - :white_check_mark: `LATERAL` subquery
    - :white_check_mark: Joins: `INNER`, `LEFT`, `RIGHT`, `CROSS`
    - :white_check_mark: `JOIN ... ON (expr)`
    - :x: `FROM ONLY`
    - :x: `TABLESAMPLE`
    - :x: Function calls
    - :x: `FROM t1, t2, t3, ...` (Workaround: `CROSS JOIN` )
    - :x: `NATURAL JOIN`
    - :x: `JOIN ... USING (...)`

-  :white_check_mark: `WHERE (expr)`
```csharp
Sql
.From<Customer>()
.Where(x => x.LastName == "Kahlo")
```

- :full_moon: `GROUP BY`
    - :white_check_mark: `GROUP BY (expr, ...)`
    ```csharp
    Sql
    .From<Customer>()
    .Select(x => x)
    .GroupBy(x => x.Name).ThenBy(x => x.LastName)
    ```
    - :x: `GROUP BY ()`
    - :x: `ROLL UP`, `CUBE`, `GROUPING SETS`
- :x: `HAVING`
- :full_moon: `WINDOW`
    - Details in [WINDOW supported syntax](./window.md)
    - :white_check_mark: `PARTITION BY`
    - :full_moon: `ORDER BY`
        - :white_check_mark: `ASC | DESC`
        - :white_check_mark: `NULLS [FIRST | LAST]`
        - :x: `ORDER BY ... USING ...`
    - :white_check_mark: `frame_clause`
        - :white_check_mark: `{ RANGE | ROWS } ... `
        - :white_check_mark: `{ RANGE | ROWS } BETWEEN ... AND ...`
    - :x: Define `WINDOW` based on another existing `WINDOW`
- :x: `UNION`, `INTERSECT`, `EXCEPT`
- :white_check_mark: `LIMIT (expr)`
    ```csharp
     Sql
    .From<Customer>()
    .Limit(100)
    ```
- :x: `OFFSET ...`
- :x: `FETCH (...)`
- :x: `FOR ... OF ...`
