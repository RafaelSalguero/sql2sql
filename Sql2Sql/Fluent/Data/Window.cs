using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Fluent.Data
{
    public class SqlWindowCreator<TIn, TWin>
    {
    }


    public interface ISqlWindow
    {
        SqlWindowClause Current { get; }
    }
    public interface ISqlWindowBuilder : ISqlWindow
    {
        WindowClauses Input { get; }
    }

    public interface ISqlWindowBuilder<TIn, TWin> : ISqlWindowBuilder { }

    public class SqlWindowBuilder<TIn, TWin> : ISqlWindowBuilder<TIn, TWin>, ISqlWindowInterface<TIn, TWin>
    {
        public SqlWindowBuilder(WindowClauses input, SqlWindowClause current)
        {
            Input = input;
            Current = current;
        }

        public WindowClauses Input { get; }
        public SqlWindowClause Current { get; }
    }

    public class WindowClauses
    {
        public WindowClauses(object windows)
        {
            Windows = windows;
        }

        /// <summary>
        /// An object where each property is a named WINDOW. If this is an string, indicartes that this is a raw SQL  window clause
        /// </summary>
        public object Windows { get; }
    }

    public interface ISqlWindowFrameAble<TIn, TWin> : ISqlWindowBuilder<TIn, TWin> { }
    public interface ISqlWindowOrderByThenByAble<TIn, TWin> : ISqlWindowFrameAble<TIn, TWin> { }
    public interface ISqlWindowOrderByAble<TIn, TWin> : ISqlWindowFrameAble<TIn, TWin> { }
    public interface ISqlWindowPartitionByThenByAble<TIn, TWin> : ISqlWindowOrderByAble<TIn, TWin> { }
    public interface ISqlWindowPartitionByAble<TIn, TWin> : ISqlWindowOrderByAble<TIn, TWin> { }
    public interface ISqlWindowExistingAble<TIn, TWin> : ISqlWindowPartitionByAble<TIn, TWin> { }
    public interface ISqlWindowItemAble<TIn, TWin> : ISqlWindowPartitionByAble<TIn, TWin> { }


    public interface ISqlWindowInterface<TIn, TWin> :
        ISqlWindowOrderByThenByAble<TIn, TWin>, ISqlWindowOrderByAble<TIn, TWin>, ISqlWindowPartitionByThenByAble<TIn, TWin>, ISqlWindowPartitionByAble<TIn, TWin>,
        ISqlWindowExistingAble<TIn, TWin>, ISqlWindowFrameInterface<TIn, TWin>,
        ISqlWindowItemAble<TIn, TWin>
    {

    }

    public class SqlWindowClause
    {
        public SqlWindowClause(ISqlWindow existingWindow, IReadOnlyList<PartitionByExpr> partitionBy, IReadOnlyList<OrderByExpr> orderBy, SqlWinFrame frame)
        {
            ExistingWindow = existingWindow;
            PartitionBy = partitionBy;
            OrderBy = orderBy;
            Frame = frame;
        }

        public SqlWindowClause SetPartitionBy(IReadOnlyList<PartitionByExpr> partitionBy) =>
             new SqlWindowClause(ExistingWindow, partitionBy, OrderBy, Frame);

        public SqlWindowClause SetFrame(SqlWinFrame frame) =>
              new SqlWindowClause(ExistingWindow, PartitionBy, OrderBy, frame);

        public SqlWindowClause SetOrderBy(IReadOnlyList<OrderByExpr> orderBy) =>
             new SqlWindowClause(ExistingWindow, PartitionBy, orderBy, Frame);


        public ISqlWindow ExistingWindow { get; }
        /// <summary>
        /// PARTITION BY clauses. Can be null
        /// </summary>
        public IReadOnlyList<PartitionByExpr> PartitionBy { get; }

        /// <summary>
        /// ORDER BY clauses. Can be null
        /// </summary>
        public IReadOnlyList<OrderByExpr> OrderBy { get; }
        public SqlWinFrame Frame { get; }
    }
}
