using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlToSql.Fluent
{
    //El proposito de usar JoinTuple<T> en lugar de Tuple<T> es eliminar la ambigüedad en las llamadas al metodo On

    public interface IJoinTuple { }
    public class JoinTuple<T1> : IJoinTuple
    {
        public JoinTuple(T1 item1)
        {
            Item1 = item1;
        }

        public T1 Item1 { get; }
    }

    public class JoinTuple<T1, T2> : JoinTuple<T1>
    {
        public JoinTuple(T1 item1, T2 item2) : base(item1)
        {
            Item2 = item2;
        }
        public T2 Item2 { get; }
    }

    public class JoinTuple<T1, T2, T3> : JoinTuple<T1, T2>
    {
        public JoinTuple(T1 item1, T2 item2, T3 item3) : base(item1, item2)
        {
            Item3 = item3;
        }
        public T3 Item3 { get; }
    }

    public class JoinTuple<T1, T2, T3, T4> : JoinTuple<T1, T2, T3>
    {
        public JoinTuple(T1 item1, T2 item2, T3 item3, T4 item4) : base(item1, item2, item3)
        {
            Item4 = item4;
        }
        public T4 Item4 { get; }
    }

    public class JoinTuple<T1, T2, T3, T4, T5> : JoinTuple<T1, T2, T3, T4>
    {
        public JoinTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) : base(item1, item2, item3, item4)
        {
            Item5 = item5;
        }
        public T5 Item5 { get; }
    }

    public class JoinTuple<T1, T2, T3, T4, T5, T6> : JoinTuple<T1, T2, T3, T4, T5>
    {
        public JoinTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) : base(item1, item2, item3, item4, item5)
        {
            Item6 = item6;
        }
        public T6 Item6 { get; }
    }
}
