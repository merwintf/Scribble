using System; using System.Collections.Generic; using System.Collections.ObjectModel; using System.Collections.Specialized; using System.ComponentModel; using System.Linq; using System.Windows.Threading;

public class SmartObservableCollection<T> : ObservableCollection<T> { private readonly Dispatcher _dispatcher; private bool _suppressNotifications = false; private bool _pendingReset = false;

public SmartObservableCollection()
{
    _dispatcher = Dispatcher.CurrentDispatcher;
}

public SmartObservableCollection(IEnumerable<T> collection) : this()
{
    foreach (var item in collection)
        base.Add(item);
}

private void RunOnUIThread(Action action, DispatcherPriority priority = DispatcherPriority.DataBind)
{
    if (_dispatcher.CheckAccess())
        action();
    else
        _dispatcher.BeginInvoke(action, priority);
}

private void NotifyResetSmart()
{
    if (_suppressNotifications)
    {
        _pendingReset = true;
        return;
    }

    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
    OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
}

public IDisposable SuspendNotifications()
{
    _suppressNotifications = true;
    return new NotificationScope(this);
}

private class NotificationScope : IDisposable
{
    private readonly SmartObservableCollection<T> _collection;
    public NotificationScope(SmartObservableCollection<T> collection) => _collection = collection;

    public void Dispose()
    {
        _collection._suppressNotifications = false;
        if (_collection._pendingReset)
        {
            _collection._pendingReset = false;
            _collection.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            _collection.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}

public new void Add(T item)
{
    RunOnUIThread(() => base.Add(item));
}

public new bool Remove(T item)
{
    bool result = false;
    RunOnUIThread(() => result = base.Remove(item));
    return result;
}

public new void Clear()
{
    RunOnUIThread(() =>
    {
        base.Clear();
        NotifyResetSmart();
    });
}

public new void Insert(int index, T item)
{
    RunOnUIThread(() => base.Insert(index, item));
}

public new void RemoveAt(int index)
{
    RunOnUIThread(() => base.RemoveAt(index));
}

public new T this[int index]
{
    get => base[index];
    set => RunOnUIThread(() => base[index] = value);
}

public void AddRange(IEnumerable<T> items)
{
    if (items == null) throw new ArgumentNullException(nameof(items));

    RunOnUIThread(() =>
    {
        foreach (var item in items)
            Items.Add(item);

        NotifyResetSmart();
    });
}

public void RemoveRange(IEnumerable<T> items)
{
    if (items == null) throw new ArgumentNullException(nameof(items));

    RunOnUIThread(() =>
    {
        foreach (var item in items)
            Items.Remove(item);

        NotifyResetSmart();
    });
}

public void AddSorted(T item, IComparer<T>? comparer = null)
{
    RunOnUIThread(() =>
    {
        comparer ??= Comparer<T>.Default;
        if (comparer == null)
            throw new InvalidOperationException($"Type {typeof(T)} must implement IComparable<T> or a comparer must be provided.");

        int index = Items.BinarySearchInsertIndex(item, comparer);
        base.Insert(index, item);
    });
}

public void AddRangeSorted(IEnumerable<T> newItems, IComparer<T>? comparer = null)
{
    if (newItems == null) throw new ArgumentNullException(nameof(newItems));

    RunOnUIThread(() =>
    {
        comparer ??= Comparer<T>.Default;
        if (comparer == null)
            throw new InvalidOperationException($"Type {typeof(T)} must implement IComparable<T> or a comparer must be provided.");

        var sortedNew = newItems.ToList();
        sortedNew.Sort(comparer);

        var merged = new List<T>(Items.Count + sortedNew.Count);
        int i = 0, j = 0;

        while (i < Items.Count && j < sortedNew.Count)
        {
            if (comparer.Compare(Items[i], sortedNew[j]) <= 0)
                merged.Add(Items[i++]);
            else
                merged.Add(sortedNew[j++]);
        }

        while (i < Items.Count) merged.Add(Items[i++]);
        while (j < sortedNew.Count) merged.Add(sortedNew[j++]);

        Items.Clear();
        foreach (var item in merged)
            Items.Add(item);

        NotifyResetSmart();
    });
}

}

public static class ListExtensions { public static int BinarySearchInsertIndex<T>(this IList<T> list, T item, IComparer<T> comparer) { int low = 0, high = list.Count; while (low < high) { int mid = (low + high) / 2; if (comparer.Compare(list[mid], item) < 0) low = mid + 1; else high = mid; } return low; } }

