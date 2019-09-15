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
        ISqlWindowClause Current { get; }
    }
    public interface ISqlWindowBuilder : ISqlWindow
    {
        IWindowClauses Window { get; }
    }

    public interface ISqlWindowBuilder<TIn,  TWin>: ISqlWindowBuilder
    {
        WindowClauses<TWin> Input { get; }
        SqlWindowClause<TIn, TWin> Current { get; }
    }

    public class SqlWindowBuilder<TIn, TWin> : ISqlWindowBuilder<TIn, TWin>, ISqlWindowInterface<TIn, TWin>
    {
        public SqlWindowBuilder(WindowClauses<TWin> input, SqlWindowClause<TIn, TWin> current)
        {
            Input = input;
            Current = current;
        }

        public WindowClauses<TWin> Input { get; }
        public SqlWindowClause<TIn, TWin> Current { get; }

        public IWindowClauses Window => Window;
        ISqlWindowClause ISqlWindow.Current => Current;
    }

    public interface IWindowClauses
    {
        object Windows { get; }
    }

    public class WindowClauses<TWin> : IWindowClauses
    {
        public WindowClauses(TWin windows)
        {
            Windows = windows;
        }

        /// <summary>
        /// Un objeto donde cada propiedad es un WINDOW, en caso de que este tipo sea string, indica que es un SQL Raw
        /// </summary>
        public TWin Windows { get; }
        object IWindowClauses.Windows => Windows;
    }

    public interface ISqlWindowFrameAble<TIn, TWin>: ISqlWindowBuilder<TIn, TWin> { }
    public interface ISqlWindowOrderByThenByAble<TIn, TWin> : ISqlWindowFrameAble<TIn, TWin> { }
    public interface ISqlWindowOrderByAble<TIn, TWin> : ISqlWindowFrameAble<TIn, TWin> { }
    public interface ISqlWindowPartitionByThenByAble<TIn, TWin> : ISqlWindowOrderByAble<TIn, TWin> { }
    public interface ISqlWindowPartitionByAble<TIn, TWin> : ISqlWindowOrderByAble<TIn, TWin> { }
    public interface ISqlWindowExistingAble<TIn, TWin> : ISqlWindowPartitionByAble<TIn, TWin> {  }


    public interface ISqlWindowInterface<TIn, TWin> :
        ISqlWindowOrderByThenByAble<TIn, TWin>, ISqlWindowOrderByAble<TIn, TWin>, ISqlWindowPartitionByThenByAble<TIn, TWin>, ISqlWindowPartitionByAble<TIn, TWin>,
        ISqlWindowExistingAble<TIn, TWin>, ISqlWindowFrameInterface<TIn, TWin>
    {

    }
  

    public interface ISqlWindowClause
    {
        ISqlWindow ExistingWindow { get; }
        IReadOnlyList<IPartitionBy> PartitionBy { get; }
        IReadOnlyList<IOrderByExpr> OrderBy { get; }
        SqlWinFrame Frame { get; }
    }

    public class SqlWindowClause<TIn, TWin> : ISqlWindowClause 
    {
        public SqlWindowClause(ISqlWindow existingWindow, IReadOnlyList<PartitionByExpr<TIn>> partitionBy, IReadOnlyList<OrderByExpr<TIn>> orderBy, SqlWinFrame frame)
        {
            ExistingWindow = existingWindow;
            PartitionBy = partitionBy;
            OrderBy = orderBy;
            Frame = frame;
        }

        public SqlWindowClause<TIn, TWin> SetPartitionBy(IReadOnlyList<PartitionByExpr<TIn>> partitionBy) =>
             new SqlWindowClause<TIn, TWin>(ExistingWindow, partitionBy, OrderBy, Frame);

        public SqlWindowClause<TIn, TWin> SetFrame(SqlWinFrame frame) =>
              new SqlWindowClause<TIn, TWin>(ExistingWindow, PartitionBy, OrderBy, frame);

        public SqlWindowClause<TIn, TWin> SetOrderBy(IReadOnlyList<OrderByExpr<TIn>> orderBy) =>
             new SqlWindowClause<TIn, TWin>(ExistingWindow, PartitionBy, orderBy, Frame);


        public ISqlWindow ExistingWindow { get; }
        public IReadOnlyList<PartitionByExpr<TIn>> PartitionBy { get; }
        public IReadOnlyList<OrderByExpr<TIn>> OrderBy { get; }
        public SqlWinFrame Frame { get; }

        IReadOnlyList<IPartitionBy> ISqlWindowClause.PartitionBy => PartitionBy;
        IReadOnlyList<IOrderByExpr> ISqlWindowClause.OrderBy => OrderBy;
    }
}
