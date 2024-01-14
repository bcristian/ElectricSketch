using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib
{
    public class ReadOnlySet<T>(ISet<T> set) : ISet<T>
    {
        readonly ISet<T> set = set;

        public int Count => set.Count;
        public bool IsReadOnly => true;
        public bool Add(T item) => throw new NotSupportedException("read only");
        public void Clear() => throw new NotSupportedException("read only");
        public bool Contains(T item) => set.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => set.CopyTo(array, arrayIndex);
        public void ExceptWith(IEnumerable<T> other) => throw new NotSupportedException("read only");
        public IEnumerator<T> GetEnumerator() => set.GetEnumerator();
        public void IntersectWith(IEnumerable<T> other) => throw new NotSupportedException("read only");
        public bool IsProperSubsetOf(IEnumerable<T> other) => set.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => set.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => set.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => set.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => set.Overlaps(other);
        public bool Remove(T item) => throw new NotSupportedException("read only");
        public bool SetEquals(IEnumerable<T> other) => set.SetEquals(other);
        public void SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException("read only");
        public void UnionWith(IEnumerable<T> other) => throw new NotSupportedException("read only");
        void ICollection<T>.Add(T item) => throw new NotSupportedException("read only");
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)set).GetEnumerator();
    }

    public static class ReadOnlySetExt
    {
        public static ReadOnlySet<T> AsReadOnly<T>(this ISet<T> set) => new(set);
    }
}
