﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sql2Sql.Fluent;
using Sql2Sql.Fluent.Data;

namespace Sql2Sql
{
    /// <summary>
    /// Una instrucción de SQL. No necesariamente genera resultados
    /// </summary>
    public interface ISqlStatement { }

    /// <summary>
    /// Una instrucción de SQL que genera un resultado de cierto tipo
    /// </summary>
    public interface ISqlQuery : ISqlStatement { }

    /// <summary>
    /// Una instrucción de SQL que genera un resultado de cierto tipo
    /// </summary>
    public interface ISqlQuery <out TOut>: ISqlStatement { }

    /// <summary>
    /// Un SELECT
    /// </summary>
    public interface ISqlSelect : ISqlQuery, IFromListItemTarget { }

    /// <summary>
    /// Un SELECT
    /// </summary>
    public interface ISqlSelect<out T> : ISqlQuery<T>, IFromListItemTarget<T>, ISqlSelect
    {
    }

    public interface ISqlWithSelect : ISqlSelect
    {
        WithSelectClause With { get; }
        ISqlSelect Query { get; }
    }

    /// <summary>
    /// Un WITH ... SELECT
    /// </summary>
    public interface ISqlWithSubquery<out T> : ISqlSelect<T>, ISqlWithSelect
    {
        new WithSelectClause With { get; }
        new ISqlSelect<T> Query { get; }
    }
}
