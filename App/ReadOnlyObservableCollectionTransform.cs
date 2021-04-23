using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ElectricSketch
{
    /// <summary>
    /// Provides a read-only wrapper over an <see cref="ObservableCollection{T}"/>, using a transformation from the original item type to another.
    /// </summary>
    public class ReadOnlyObservableCollectionTransform<OriginalType, TransformedType> : IReadOnlyObservableCollection<TransformedType>
    {
        public ReadOnlyObservableCollectionTransform(ReadOnlyObservableCollection<OriginalType> observable, Func<OriginalType, TransformedType> transform, bool useWeakReferences)
        {
            this.transform = transform;
            original = observable;

            if (useWeakReferences)
            {
                CollectionChangedEventManager.AddHandler(original, OnOriginalCollectionChanged);
                PropertyChangedEventManager.AddHandler(original, (_, e) => PropertyChanged?.Invoke(this, e), string.Empty);
            }
            else
            {
                ((INotifyCollectionChanged)original).CollectionChanged += OnOriginalCollectionChanged;
                ((INotifyPropertyChanged)original).PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
            }

            transformed = new List<TransformedType>(original.Select(i => transform(i)));
        }

        private void OnOriginalCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex < 0)
                        foreach (OriginalType item in e.NewItems)
                            transformed.Add(transform(item));
                    else
                        for (int i = 0; i < e.NewItems.Count; i++)
                            transformed.Insert(e.NewStartingIndex + i, transform((OriginalType)e.NewItems[i]));
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                        transformed.GetRange(e.NewStartingIndex, e.NewItems.Count)));
                    return;

                case NotifyCollectionChangedAction.Remove:
                    {
                        var removed = new ArrayList(e.OldItems.Count);
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            removed.Add(transformed[i + e.OldStartingIndex]);
                            transformed.RemoveAt(i + e.OldStartingIndex);
                        }
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                            removed, e.OldStartingIndex));
                        return;
                    }

                case NotifyCollectionChangedAction.Replace:
                    {
                        var removed = new ArrayList(e.NewItems.Count);
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            removed.Add(transformed[i + e.NewStartingIndex]);
                            transformed[i] = transform((OriginalType)e.NewItems[i]);
                        }
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                            transformed.GetRange(e.NewStartingIndex, e.NewItems.Count), removed, e.NewStartingIndex));
                        return;
                    }

                case NotifyCollectionChangedAction.Move:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        var item = transformed[i + e.OldStartingIndex];
                        transformed.RemoveAt(i + e.OldStartingIndex);
                        transformed.Insert(i + e.NewStartingIndex, item);
                    }
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move,
                        transformed.GetRange(e.NewStartingIndex, e.NewItems.Count), e.NewStartingIndex, e.OldStartingIndex));
                    return;

                case NotifyCollectionChangedAction.Reset:
                    // Rebuild the entire list.
                    transformed.Clear();
                    foreach (var item in original)
                        transformed.Add(transform(item));
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    return;

            }
            throw new InvalidOperationException();
        }

        readonly ReadOnlyObservableCollection<OriginalType> original;
        readonly List<TransformedType> transformed;
        readonly Func<OriginalType, TransformedType> transform;



        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public int Count => original.Count;
        public bool IsReadOnly => true;
        public bool IsFixedSize => ((IList)original).IsFixedSize;

        public bool IsSynchronized => false;
        public object SyncRoot => ((ICollection)transformed).SyncRoot;

        object IList.this[int index]
        {
            get => transformed[index];
            set => throw new NotSupportedException();
        }
        TransformedType IList<TransformedType>.this[int index]
        {
            get => transformed[index];
            set => throw new NotSupportedException();
        }

        public TransformedType this[int index] => transformed[index];

        public IEnumerator<TransformedType> GetEnumerator() => transformed.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)transformed).GetEnumerator();

        public int IndexOf(TransformedType item) => transformed.IndexOf(item);
        int IList.IndexOf(object value) => ((IList)transformed).IndexOf(value);

        public void Add(TransformedType item) => throw new NotSupportedException();
        int IList.Add(object value) => throw new NotSupportedException();
        public void Insert(int index, TransformedType item) => throw new NotSupportedException();
        void IList.Insert(int index, object value) => throw new NotSupportedException();
        public bool Remove(TransformedType item) => throw new NotSupportedException();
        void IList.Remove(object value) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();

        public bool Contains(TransformedType item) => transformed.Contains(item);
        bool IList.Contains(object value) => ((IList)transformed).Contains(value);

        public void CopyTo(TransformedType[] array, int arrayIndex) => transformed.CopyTo(array, arrayIndex);
        void ICollection.CopyTo(Array array, int index) => ((ICollection)transformed).CopyTo(array, index);
    }

    public interface IReadOnlyObservableCollection<T> : IList<T>, IReadOnlyList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
    }
}
