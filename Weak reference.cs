public class ParentViewModel
{
    private event EventHandler<string> SomeEvent;

    public void RaiseEvent(string message)
    {
        SomeEvent?.Invoke(this, message);
    }

    public ChildViewModel CreateChild()
    {
        var initData = new Dictionary<string, object>
        {
            { "AddEventHandler", new Action<EventHandler<string>>(handler => SomeEvent += handler) },
            { "Key", "SomeValue" } // Example additional data
        };
        return new ChildViewModel(initData);
    }
}

public class ChildViewModel
{
    private readonly Dictionary<string, object> _initData;
    private readonly WeakReference<Action<object, string>> _handlerRef;

    public ChildViewModel(Dictionary<string, object> initData)
    {
        _initData = initData ?? new Dictionary<string, object>();
        _handlerRef = new WeakReference<Action<object, string>>(OnParentEvent);

        // Extract the add handler method from initData and subscribe
        if (_initData.TryGetValue("AddEventHandler", out var addHandlerObj) && addHandlerObj is Action<EventHandler<string>> addHandler)
        {
            if (_handlerRef.TryGetTarget(out var handler))
            {
                addHandler((sender, args) => handler(sender, args));
            }
        }
    }

    private void OnParentEvent(object sender, string message)
    {
        Console.WriteLine($"Child received event with message: {message}");
        if (_initData.TryGetValue("Key", out var value))
        {
            Console.WriteLine($"Init data value: {value}");
        }
    }
}
