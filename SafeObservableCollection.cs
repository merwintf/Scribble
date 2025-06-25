using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

public class SafeObservableCollection<T> : INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly ObservableCollection<T> _collection;
    private readonly Dispatcher _dispatcher;
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    private bool _suppressNotifications = false;
    private bool _pendingReset = false;
    private const string IndexerName = "Item[]";

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeObservableCollection{T}"/> class.
    /// </summary>
    public SafeObservableCollection()
    {
        _collection = new ObservableCollection<T>();
        _dispatcher = Dispatcher.CurrentDispatcher;
        _collection.CollectionChanged += (s, e) => OnCollectionChanged(e);
        _collection.PropertyChanged += (s, e) => OnPropertyChanged(e);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeObservableCollection{T}"/> class with the specified collection.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new collection.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null or contains null elements (if <typeparamref name="T"/> is a reference type).</exception>
    public SafeObservableCollection(IEnumerable<T> collection) : this()
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        foreach (var item in collection)
        {
            if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(collection), "Collection contains null elements.");
            _collection.Add(item);
        }
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotifications)
            CollectionChanged?.Invoke(this, e);
    }

    private void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_suppressNotifications)
            PropertyChanged?.Invoke(this, e);
    }

    // Synchronous UI thread invocation
    private void RunOnUIThread(Action action, DispatcherPriority priority = DispatcherPriority.DataBind)
    {
        try
        {
            if (_dispatcher.CheckAccess())
                action();
            else
                _dispatcher.Invoke(action, priority);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to execute action on UI thread.", ex);
        }
    }

    // Asynchronous UI thread invocation
    private async Task RunOnUIThreadAsync(Action action, DispatcherPriority priority = DispatcherPriority.DataBind)
    {
        try
        {
            if (_dispatcher.CheckAccess())
                action();
            else
                await _dispatcher.InvokeAsync(action, priority);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to execute action on UI thread.", ex);
        }
    }

    // Synchronous UI thread invocation with return value
    private T RunOnUIThread<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.DataBind)
    {
        try
        {
            if (_dispatcher.CheckAccess())
                return func();
            return (T)_dispatcher.Invoke(func, priority);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("508bFailed to execute function on UI thread.", ex);
        }
    }

    // Asynchronous UI thread invocation with return value
    private async Task<T> RunOnUIThreadAsync<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.DataBind)
    {
        try
        {
            if (_dispatcher.CheckAccess())
                return func();
            return await _dispatcher.InvokeAsync(func, priority);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to execute function on UI thread.", ex);
        }
    }

    private void NotifyResetSmart()
    {
        if (_suppressNotifications)
        {
            _pendingReset = true;
            return;
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs(IndexerName));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Suspends collection change notifications until the returned object is disposed.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that, when disposed, resumes notifications and triggers a reset if changes occurred.</returns>
    public IDisposable SuspendNotifications()
    {
        _suppressNotifications = true;
        return new NotificationScope(this);
    }

    private class NotificationScope : IDisposable
    {
        private readonly SafeObservableCollection<T> _collection;

        public NotificationScope(SafeObservableCollection<T> collection) => _collection = collection;

        public void Dispose()
        {
            _collection._suppressNotifications = false;
            if (_collection._pendingReset)
            {
                _collection._pendingReset = false;
                _collection.OnPropertyChanged(new PropertyChangedEventArgs(IndexerName));
                _collection.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }

    /// <summary>
    /// Gets the number of elements in the collection.
    /// </summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _collection.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Adds an item to the collection (synchronous).
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null and <typeparamref name="T"/> is a reference type.</exception>
    /// <remarks>
    /// This method is synchronous and may block if called from a non-UI thread.
    /// Use <see cref="AddAsync(T)"/> for non-blocking operations.
    /// </remarks>
    public void Add(T item)
    {
        if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(item));
        RunOnUIThread(() => _collection.Add(item));
    }

    /// <summary>
    /// Adds an item to the collection (asynchronous).
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null and <typeparamref name="T"/> is a reference type.</exception>
    public async Task AddAsync(T item)
    {
        if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(item));
        await RunOnUIThreadAsync(() => _collection.Add(item));
    }

    /// <summary>
    /// Removes the specified item from the collection (synchronous).
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>True if the item was removed; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null and <typeparamref name="T"/> is a reference type.</exception>
    /// <remarks>
    /// This method is synchronous and may block if called from a non-UI thread.
    /// Use <see cref="RemoveAsync(T)"/> for non-blocking operations.
    /// </remarks>
    public bool Remove(T item)
    {
        if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(item));
        return RunOnUIThread(() => _collection.Remove(item));
    }

    /// <summary>
    /// Removes the specified item from the collection (asynchronous).
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>True if the item was removed; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null and <typeparamref name="T"/> is a reference type.</exception>
    public async Task<bool> RemoveAsync(T item)
    {
        if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(item));
        return await RunOnUIThreadAsync(() => _collection.Remove(item));
    }

    /// <summary>
    /// Removes all items from the collection (synchronous).
    /// </summary>
    /// <remarks>
    /// This method is synchronous and may block if called from a non-UI thread.
    /// Use <see cref="ClearAsync"/> for non-blocking operations.
    /// </remarks>
    public void Clear()
    {
        RunOnUIThread(() => _collection.Clear());
    }

    /// <summary>
    /// Removes all items from the collection (asynchronous).
    /// </summary>
    public async Task ClearAsync()
    {
        await RunOnUIThreadAsync(() => _collection.Clear());
    }

    /// <summary>
    /// Inserts an item at the specified index (synchronous).
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the item.</param>
    /// <param name="item">The item to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null and <typeparamref name="T"/> is a reference type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
    /// <remarks>
    /// This method is synchronous and may block if called from a non-UI thread.
    /// Use <see cref="InsertAsync(int, T)"/> for non-blocking operations.
    /// </remarks>
    public void Insert(int index, T item)
    {
        if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(item));
        _lock.EnterReadLock();
        try
        {
            if (index < 0 || index > _collection.Count) throw new ArgumentOutOfRangeException(nameof(index));
        }
        finally
        {
            _lock.ExitReadLock();
        }
        RunOnUIThread(() => _collection.Insert(index, item));
    }

    /// <summary>
    /// Inserts an item at the specified index (asynchronous).
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the item.</param>
    /// <param name="item">The item to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null and <typeparamref name="T"/> is a reference type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
    public async Task InsertAsync(int index, T item)
    {
        if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(item));
        _lock.EnterReadLock();
        try
        {
            if (index < 0 || index > _collection.Count) throw new ArgumentOutOfRangeException(nameof(index));
        }
        finally
        {
            _lock.ExitReadLock();
        }
        await RunOnUIThreadAsync(() => _collection.Insert(index, item));
    }

    /// <summary>
    /// Removes the item at the specified index (synchronous).
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
    /// <remarks>
    /// This method is synchronous and may block if called from a non-UI thread.
    /// Use <see cref="RemoveAtAsync(int)"/> for non-blocking operations.
    /// </remarks>
    public void RemoveAt(int index)
    {
        _lock.EnterReadLock();
        try
        {
            if (index < 0 || index >= _collection.Count) throw new ArgumentOutOfRangeException(nameof(index));
        }
        finally
        {
            _lock.ExitReadLock();
        }
        RunOnUIThread(() => _collection.RemoveAt(index));
    }

    /// <summary>
    /// Removes the item at the specified index (asynchronous).
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
    public async Task RemoveAtAsync(int index)
    {
        _lock.EnterReadLock();
        try
        {
            if (index < 0 || index >= _collection.Count) throw new ArgumentOutOfRangeException(nameof(index));
        }
        finally
        {
            _lock.ExitReadLock();
        }
        await RunOnUIThreadAsync(() => _collection.RemoveAt(index));
    }

    /// <summary>
    /// Gets or sets the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    /// <exception cref="ArgumentNullException">Thrown when setting a null <paramref name="value"/> and <typeparamref name="T"/> is a reference type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is out of range.</exception>
    /// <remarks>
    /// The setter is synchronous and may block if called from a non-UI thread.
    /// </remarks>
    public T this[int index]
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                if (index < 0 || index >= _collection.Count) throw new ArgumentOutOfRangeException(nameof(index));
                return _collection[index];
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        set
        {
            if (value == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(value));
            _lock.EnterReadLock();
            try
            {
                if (index < 0 || index >= _collection.Count) throw new ArgumentOutOfRangeException(nameof(index));
            }
            finally
            {
                _lock.ExitReadLock();
            }
            RunOnUIThread(() => _collection[index] = value);
        }
    }

    /// <summary>
    /// Adds a range of items to the collection (synchronous).
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null or contains null elements (if <typeparamref name="T"/> is a reference type).</exception>
    /// <remarks>
    /// This method is synchronous and may block if called from a non-UI thread.
    /// Use <see cref="AddRangeAsync(IEnumerable{T})"/> for non-blocking operations.
    /// </remarks>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        RunOnUIThread(() =>
        {
            using (SuspendNotifications())
            {
                foreach (var item in items)
                {
                    if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(items), "Collection contains null elements.");
                    _collection.Add(item);
                }
            }
            NotifyResetSmart();
        });
    }

    /// <summary>
    /// Adds a range of items to the collection (asynchronous).
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null or contains null elements (if <typeparamref name="T"/> is a reference type).</exception>
    public async Task AddRangeAsync(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        await RunOnUIThreadAsync(() =>
        {
            using (SuspendNotifications())
            {
                foreach (var item in items)
                {
                    if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(items), "Collection contains null elements.");
                    _collection.Add(item);
                }
            }
            NotifyResetSmart();
        });
    }

    /// <summary>
    /// Removes a range of items from the collection (synchronous).
    /// </summary>
    /// <param name="items">The items to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
    /// <remarks>
    /// This method is synchronous and may block if called from a non-UI thread.
    /// Use <see cref="RemoveRangeAsync(IEnumerable{T})"/> for non-blocking operations.
    /// </remarks>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        RunOnUIThread(() =>
        {
            using (SuspendNotifications())
            {
                foreach (var item in items)
                {
                    _collection.Remove(item);
                }
            }
            NotifyResetSmart();
        });
    }

    /// <summary>
    /// Removes a range of items from the collection (asynchronous).
    /// </summary>
    /// <param name="items">The items to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
    public async Task RemoveRangeAsync(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        await RunOnUIThreadAsync(() =>
        {
            using (SuspendNotifications())
            {
                foreach (var item in items)
                {
                    _collection.Remove(item);
                }
            }
            NotifyResetSmart();
        });
    }

    /// <summary>
    /// Adds an item to the collection in sorted order (synchronous).
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <param name="comparer">The comparer to determine the sort order (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null and <typeparamref name="T"/> is a reference type.</exception>
    /// <remarks>
    /// This method is synchronous and may block if called from a non-UI thread.
    /// Use <see cref="AddSortedAsync(T, IComparer{T})"/> for non-blocking operations.
    /// </remarks>
    public void AddSorted(T item, IComparer<T> comparer = null)
    {
        if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(item));
        comparer ??= Comparer<T>.Default;
        RunOnUIThread(() =>
        {
            int index = _collection.BinarySearchInsertIndex(item, comparer);
            _collection.Insert(index, item);
        });
    }

    /// <summary>
    /// Adds an item to the collection in sorted order (asynchronous).
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <param name="comparer">The comparer to determine the sort order (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null and <typeparamref name="T"/> is a reference type.</exception>
    public async Task AddSortedAsync(T item, IComparer<T> comparer = null)
    {
        if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(item));
        comparer ??= Comparer<T>.Default;
        await RunOnUIThreadAsync(() =>
        {
            int index = _collection.BinarySearchInsertIndex(item, comparer);
            _collection.Insert(index, item);
        });
    }

    /// <summary>
    /// Adds a range of items to the collection in sorted order (synchronous).
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <param name="comparer">The comparer to determine the sort order (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null or contains null elements (if <typeparamref name="T"/> is a reference type).</exception>
    /// <remarks>
    /// This method is synchronous and may block if called from a non-UI thread.
    /// Use <see cref="AddRangeSortedAsync(IEnumerable{T}, IComparer{T})"/> for non-blocking operations.
    /// </remarks>
    public void AddRangeSorted(IEnumerable<T> items, IComparer<T> comparer = null)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        comparer ??= Comparer<T>.Default;
        RunOnUIThread(() => _dispatcher.Invoke(() => AddRangeSortedInternal(items, comparer)));
    }

    private void AddRangeSortedInternal(IEnumerable<T> items, IComparer<T> comparer)
    {
        using (SuspendNotifications())
        {
            foreach (var item in items.OrderBy(i => i, comparer))
            {
                if (item == null && !typeof(T).IsValueType) throw new ArgumentNullException(nameof(items), "Collection contains null elements.");
                int index = _collection.BinarySearchInsertIndex(item, comparer);
                _collection.Insert(index, item);
            }
        }
        NotifyResetSmart();
    }

    /// <summary>
    /// Adds a range of items to the collection in sorted order (asynchronous).
    /// </summary>
    /// <param name="items">The items to add.</param>
    /// <param name="comparer">The comparer to determine the sort order (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null or contains null elements (if <typeparamref name="T"/> is a reference type).</exception>
    public async Task AddRangeSortedAsync(IEnumerable<T> items, IComparer<T> comparer = null)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        comparer ??= Comparer<T>.Default;
        await RunOnUIThreadAsync(() => AddRangeSortedInternal(items, comparer));
    }
}

// Extension method for binary search insert index
public static class ListExtensions
{
    public static int BinarySearchInsertIndex<T>(this IList<T> list, T item, IComparer<T> comparer)
    {
        int low = 0, high = list.Count;
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (comparer.Compare(list[mid], item) < 0)
                low = mid + 1;
            else
                high = mid;
        }
        return low;
    }
}