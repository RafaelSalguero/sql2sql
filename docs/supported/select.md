# `SELECT` supported syntax

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

-  :white_check_mark: `DISTINCT ON`
    ```csharp
    Sql
    .From<Customer>()
    .DistinctOn(x => x.LocationId)
    ```