using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

public class SmartObservableCollection<T> : ObservableCollection<T>
{
    private readonly Dispatcher _dispatcher;

    public SmartObservableCollection()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    public SmartObservableCollection(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_dispatcher.CheckAccess())
        {
            base.OnCollectionChanged(e);
        }
        else
        {
            _dispatcher.BeginInvoke(new Action(() => base.OnCollectionChanged(e)), DispatcherPriority.DataBind);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (_dispatcher.CheckAccess())
        {
            base.OnPropertyChanged(e);
        }
        else
        {
            _dispatcher.BeginInvoke(new Action(() => base.OnPropertyChanged(e)), DispatcherPriority.DataBind);
        }
    }

    // Override base mutating methods to ensure internal thread safety
    public new void Add(T item)
    {
        if (_dispatcher.CheckAccess())
        {
            base.Add(item);
        }
        else
        {
            _dispatcher.Invoke(() => base.Add(item));
        }
    }

    public new bool Remove(T item)
    {
        if (_dispatcher.CheckAccess())
        {
            return base.Remove(item);
        }
        else
        {
            return _dispatcher.Invoke(() => base.Remove(item));
        }
    }

    public new void Clear()
    {
        if (_dispatcher.CheckAccess())
        {
            base.Clear();
        }
        else
        {
            _dispatcher.Invoke(() => base.Clear());
        }
    }

    public new void Insert(int index, T item)
    {
        if (_dispatcher.CheckAccess())
        {
            base.Insert(index, item);
        }
        else
        {
            _dispatcher.Invoke(() => base.Insert(index, item));
        }
    }

    public new void RemoveAt(int index)
    {
        if (_dispatcher.CheckAccess())
        {
            base.RemoveAt(index);
        }
        else
        {
            _dispatcher.Invoke(() => base.RemoveAt(index));
        }
    }

    public new T this[int index]
    {
        get => base[index];
        set
        {
            if (_dispatcher.CheckAccess())
            {
                base[index] = value;
            }
            else
            {
                _dispatcher.Invoke(() => base[index] = value);
            }
        }
    }
}
