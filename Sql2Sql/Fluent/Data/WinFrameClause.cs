using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sql2Sql.Fluent.Data
{
    /// <summary>
    /// Valores para el frame_exclusion del frame_clause de un WINDOW
    /// </summary>
    public enum WinFrameExclusion{
        CurrentRow,
        Group,
        Ties,
        NoOthers
    }

    public enum WinFrameStartEnd
    {
        UnboundedPreceding,
        OffsetPreceding,
        CurrentRow,
        OffsetFollowing,
        UnboundedFollowing,
    }

    public enum WinFrameGrouping
    {
        Range,
        Rows,
        Groups
    }

    public interface ISqlWindowFrame<TIn> : ISqlWindowBuilder<TIn>
    {
    }

    public interface ISqlWindowFrameGroupingAble<TIn> : ISqlWindowFrame<TIn> { }
    public interface ISqlWindowFrameExclusionAble<TIn> : ISqlWindowFrame<TIn> {  }

    public interface ISqlWindowFrameEndAble<TIn> : ISqlWindowFrame<TIn> {  }
    public interface ISqlWindowFrameEndExclusionAble<TIn> : ISqlWindowFrameExclusionAble<TIn>, ISqlWindowFrameEndAble<TIn> { }

    public interface ISqlWindowFrameStartAble<TIn> : ISqlWindowFrame<TIn> { }
    public interface ISqlWindowFrameStartEndAble<TIn> : ISqlWindowFrameStartAble<TIn> { }
    public interface ISqlWindowFrameBetweebAble<TIn> : ISqlWindowFrame<TIn> { }

    public interface ISqlWindowFrameStartBetweenAble<TIn> : ISqlWindowFrameStartAble<TIn>, ISqlWindowFrameBetweebAble<TIn> { }

    public interface SqlWindowFrameEndAble { }

    public interface ISqlWindowFrameInterface<TIn> :
        ISqlWindowFrame<TIn>, ISqlWindowFrameGroupingAble<TIn>, ISqlWindowFrameExclusionAble<TIn>, ISqlWindowFrameStartBetweenAble<TIn>, ISqlWindowFrameStartEndAble<TIn>, ISqlWindowFrameEndExclusionAble<TIn>
    { }

    

    public class SqlWindowFrameStartEnd
    {
        public SqlWindowFrameStartEnd(WinFrameStartEnd type, int? offset)
        {
            Type = type;
            Offset = offset;
        }

        public WinFrameStartEnd Type { get; }
        public int? Offset { get; }

        public static SqlWindowFrameStartEnd UnboundedPreceding => new SqlWindowFrameStartEnd(WinFrameStartEnd.UnboundedPreceding, null);
        public static SqlWindowFrameStartEnd CurrentRow => new SqlWindowFrameStartEnd(WinFrameStartEnd.CurrentRow, null);
        public static SqlWindowFrameStartEnd UnboundedFollowing => new SqlWindowFrameStartEnd(WinFrameStartEnd.UnboundedFollowing, null);

        public static SqlWindowFrameStartEnd OffsetPreceding(int offset) => new SqlWindowFrameStartEnd(WinFrameStartEnd.OffsetPreceding, offset);
        public static SqlWindowFrameStartEnd OffsetFollowing(int offset) => new SqlWindowFrameStartEnd(WinFrameStartEnd.OffsetFollowing, offset);
    }

    /// <summary>
    /// Un frame_clause
    /// </summary>
    public class SqlWinFrame
    {
        public SqlWinFrame( WinFrameGrouping grouping, SqlWindowFrameStartEnd start, SqlWindowFrameStartEnd end, WinFrameExclusion? exclusion)
        {
            Grouping = grouping;
            Start = start;
            End = end;
            Exclusion = exclusion;
        }

        public WinFrameGrouping Grouping { get; }
        public SqlWindowFrameStartEnd Start { get; }
        public SqlWindowFrameStartEnd End { get; }
        public WinFrameExclusion? Exclusion { get; }

        public SqlWinFrame SetStart(SqlWindowFrameStartEnd start) =>
            new SqlWinFrame(  Grouping, start, End, Exclusion);

        public SqlWinFrame SetEnd(SqlWindowFrameStartEnd end) =>
            new SqlWinFrame(  Grouping, Start, end, Exclusion);

        public SqlWinFrame SetExclusion(WinFrameExclusion? exclusion) =>
            new SqlWinFrame(  Grouping, Start, End, exclusion);
    }
}
