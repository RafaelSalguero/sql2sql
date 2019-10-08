using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#if NET40
namespace System.Collections.Generic
{
    /// <summary>
    /// .NET 4.5 IReadOnlyList replacement
    /// </summary>
    /// <typeparam name="T"></typeparam>
      class IReadOnlyList<T> : IEnumerable<T>
    {
        public readonly List<T> list;

        public static implicit operator IReadOnlyList<T>(List<T> list)
        {
            return new IReadOnlyList<T>(list);
        }


        public static implicit operator IReadOnlyList<T>(T[] list)
        {
            return new IReadOnlyList<T>(list.ToList());
        }


        IReadOnlyList(List<T> list)
        {
            this.list = list;
        }

        public T this[int i] => list[i];

        public int Count => list.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)list).GetEnumerator();
        }
    }

      class IReadOnlyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        readonly Dictionary<TKey, TValue> dic;

        public IReadOnlyDictionary(Dictionary<TKey, TValue> dic)
        {
            this.dic = dic;
        }

        public static implicit operator IReadOnlyDictionary<TKey, TValue>(Dictionary<TKey, TValue> list)
        {
            return new IReadOnlyDictionary<TKey, TValue>(list);
        }


        public TValue this[TKey key] => dic[key];
        public int Count => dic.Count;
        public bool TryGetValue (TKey key, out TValue val)
        {
            
            return dic.TryGetValue(key, out val);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dic.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)dic).GetEnumerator();
        }
    }

    /// <summary>
    /// Some NET 4.5 reflection methods
    /// </summary>
      static class ReflexionNet4
    {
        public static void SetValue(this PropertyInfo prop, object instance, object value)
        {
            prop.SetValue(instance, value, null);
        }

        public static object GetValue(this PropertyInfo prop, object instance)
        {
            return prop.GetValue(instance, null);
        }


        public static T GetCustomAttribute<T>(this MemberInfo member)
        {
            return (T)member.GetCustomAttributes(true).Where(x => x is T).FirstOrDefault();
        }

      
    }
}

#endif