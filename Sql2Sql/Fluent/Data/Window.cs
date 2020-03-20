using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Fluent.Data
{
    public class SqlWindowCreator<TIn>
    {
    }


    public interface ISqlWindow
    {
        /// <summary>
        /// Previous window clause that generated this clause, is used to resolve existing windows.
        /// Is null if this clause doesn't have any parent
        /// </summary>
        ISqlWindow Previous { get; }

        /// <summary>
        /// Current clause
        /// </summary>
        SqlWindowClause Current { get; }
    }
    public interface ISqlWindowBuilder : ISqlWindow
    {
        WindowClauses Input { get; }
    }

    public interface ISqlWindowBuilder<TIn> : ISqlWindowBuilder { }

    public class SqlWindowBuilder<TIn> : ISqlWindowBuilder<TIn>, ISqlWindowInterface<TIn>
    {
        public SqlWindowBuilder(WindowClauses input, ISqlWindow previous, SqlWindowClause current)
        {
            Input = input;
            Previous = previous;
            Current = current;
        }

        public WindowClauses Input { get; }

        public ISqlWindow Previous { get; }


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

    public interface ISqlWindowFrameAble<TIn> : ISqlWindowBuilder<TIn> { }
    public interface ISqlWindowOrderByThenByAble<TIn> : ISqlWindowFrameAble<TIn> { }
    public interface ISqlWindowOrderByAble<TIn> : ISqlWindowFrameAble<TIn> { }
    public interface ISqlWindowPartitionByThenByAble<TIn> : ISqlWindowOrderByAble<TIn> { }
    public interface ISqlWindowPartitionByAble<TIn> : ISqlWindowOrderByAble<TIn> { }
    public interface ISqlWindowItemAble<TIn> : ISqlWindowPartitionByAble<TIn> { }


    public interface ISqlWindowInterface<TIn> :
        ISqlWindowOrderByThenByAble<TIn>, ISqlWindowOrderByAble<TIn>, ISqlWindowPartitionByThenByAble<TIn>, ISqlWindowPartitionByAble<TIn>,
        ISqlWindowFrameInterface<TIn>,
        ISqlWindowItemAble<TIn>
    {

    }

    public class SqlWindowClause
    {
        public SqlWindowClause(IReadOnlyList<PartitionByExpr> partitionBy, IReadOnlyList<OrderByExpr> orderBy, SqlWinFrame frame)
        {
            PartitionBy = partitionBy;
            OrderBy = orderBy;
            Frame = frame;
        }

        //Note that set methods return a new window with all other parameters set to null, this is 
        //unlike all other clause clases since internally, windows are represented as a linked list,
        //in order to identify existing window definitions

        public SqlWindowClause SetPartitionBy(IReadOnlyList<PartitionByExpr> partitionBy) =>
             new SqlWindowClause(partitionBy, null, null);

        public SqlWindowClause SetFrame(SqlWinFrame frame) =>
              new SqlWindowClause(null, null, frame);

        public SqlWindowClause SetOrderBy(IReadOnlyList<OrderByExpr> orderBy) =>
             new SqlWindowClause(null, orderBy, null);

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
