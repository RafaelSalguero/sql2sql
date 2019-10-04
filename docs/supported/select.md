# `SELECT` supported syntax

-  :white_check_mark: Select all from table
    ```csharp
    Sql.From<Customer>();
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
    .Distinct()
    ```