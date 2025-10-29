using System;
using System.Linq;
using System.Reflection;

public sealed class MobileServiceAdapter : IMobileService, IDisposable
{
    private readonly IsolatedLoadContext _alc;
    private readonly object _vendorInstance;
    private readonly MethodInfo _connect;
    private readonly MethodInfo _disconnect;
    private readonly MethodInfo _getLocation;
    private readonly EventInfo _connectedEvent;
    private readonly EventInfo _disconnectedEvent;
    private readonly Delegate _connectedProxy;
    private readonly Delegate _disconnectedProxy;

    public event EventHandler<ConnectionArgs>? Connected;
    public event EventHandler<ConnectionArgs>? Disconnected;

    /// <param name="dllDirectory">Folder that contains the vendor DLL and dependencies</param>
    /// <param name="dllFileName">Vendor DLL file name (e.g., "Vendor.Mobile.v2.dll")</param>
    /// <param name="typeFullName">Fully qualified vendor type (e.g., "Vendor.Mobile.MobileService")</param>
    public MobileServiceAdapter(string dllDirectory, string dllFileName, string typeFullName)
    {
        _alc = new IsolatedLoadContext(dllDirectory);
        var asmPath = Path.Combine(dllDirectory, dllFileName);
        var asm = _alc.LoadFromAssemblyPath(asmPath);

        var tp = asm.GetType(typeFullName, throwOnError: true)!;
        _vendorInstance = Activator.CreateInstance(tp)!;

        // Methods may be named slightly differently across versions. Probe sensibly.
        _connect = FindMethod(tp, "Connect");
        _disconnect = FindMethod(tp, "Disconnect");
        _getLocation = FindMethod(tp, "GetLocationMethod", "GetLocation", "Locate", "GetCurrentLocation");

        // Events may drift in name. Try common variants.
        _connectedEvent = FindEvent(tp, "ConnectedEvents", "Connected", "OnConnected");
        _disconnectedEvent = FindEvent(tp, "Disconnected", "OnDisconnected", "DisconnectedEvents");

        // Wire vendor → adapter events with a proxy matching vendor’s delegate type.
        _connectedProxy = CreateEventProxy(_connectedEvent.EventHandlerType!, nameof(OnVendorConnected));
        _disconnectedProxy = CreateEventProxy(_disconnectedEvent.EventHandlerType!, nameof(OnVendorDisconnected));
        _connectedEvent.AddEventHandler(_vendorInstance, _connectedProxy);
        _disconnectedEvent.AddEventHandler(_vendorInstance, _disconnectedProxy);
    }

    public void Connect()    => _connect.Invoke(_vendorInstance, Array.Empty<object?>());
    public void Disconnect() => _disconnect.Invoke(_vendorInstance, Array.Empty<object?>());

    public Location GetLocation()
    {
        // Vendor may return a custom type. Extract common fields via reflection.
        var result = _getLocation.Invoke(_vendorInstance, Array.Empty<object?>());
        if (result is null) return new Location(0, 0, null);

        double lat = GetDoubleProp(result, "Latitude") ?? GetDoubleProp(result, "Lat") ?? 0.0;
        double lon = GetDoubleProp(result, "Longitude") ?? GetDoubleProp(result, "Lng") ?? GetDoubleProp(result, "Long") ?? 0.0;
        double? acc = GetDoubleProp(result, "AccuracyMeters") ?? GetDoubleProp(result, "Accuracy");
        return new Location(lat, lon, acc);
    }

    // === Vendor→Your event bridge ===
    private void OnVendorConnected(object? sender, object? vendorArgs)
        => Connected?.Invoke(this, ToConnectionArgs(vendorArgs, isReconnectFallback: false));

    private void OnVendorDisconnected(object? sender, object? vendorArgs)
        => Disconnected?.Invoke(this, ToConnectionArgs(vendorArgs, isReconnectFallback: false));

    // Create delegate of vendor handler type bound to our private methods above.
    private Delegate CreateEventProxy(Type vendorHandlerType, string targetMethodName)
    {
        var mi = GetType().GetMethod(targetMethodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return Delegate.CreateDelegate(vendorHandlerType, this, mi);
    }

    // Robustly extract fields from unknown vendor args
    private static ConnectionArgs ToConnectionArgs(object? vendorArgs, bool isReconnectFallback)
    {
        string? deviceId = GetStringProp(vendorArgs, "DeviceId") 
                           ?? GetStringProp(vendorArgs, "Id")
                           ?? GetStringProp(vendorArgs, "IMEI");

        string? transport = GetStringProp(vendorArgs, "Transport")
                            ?? GetStringProp(vendorArgs, "Channel")
                            ?? GetStringProp(vendorArgs, "Protocol");

        bool isReconnected =
            GetBoolProp(vendorArgs, "IsReconnected")
            ?? GetBoolProp(vendorArgs, "Reconnected")
            ?? isReconnectFallback;

        return new ConnectionArgs(deviceId, transport, isReconnected);
    }

    // Helpers
    private static MethodInfo FindMethod(Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var m = t.GetMethod(n, BindingFlags.Instance | BindingFlags.Public);
            if (m != null) return m;
        }
        throw new MissingMethodException($"None of the methods found: {string.Join(", ", names)} on {t.FullName}.");
    }

    private static EventInfo FindEvent(Type t, params string[] names)
    {
        foreach (var n in names)
        {
            var e = t.GetEvent(n, BindingFlags.Instance | BindingFlags.Public);
            if (e != null) return e;
        }
        throw new MissingMemberException($"None of the events found: {string.Join(", ", names)} on {t.FullName}.");
    }

    private static string? GetStringProp(object? o, string name)
    {
        if (o is null) return null;
        var p = o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        var v = p?.GetValue(o);
        return v?.ToString();
    }

    private static bool? GetBoolProp(object? o, string name)
    {
        if (o is null) return null;
        var p = o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        var v = p?.GetValue(o);
        return v is null ? null : Convert.ToBoolean(v);
    }

    private static double? GetDoubleProp(object? o, string name)
    {
        if (o is null) return null;
        var p = o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        var v = p?.GetValue(o);
        return v is null ? null : Convert.ToDouble(v);
    }

    public void Dispose()
    {
        try
        {
            _connectedEvent.RemoveEventHandler(_vendorInstance, _connectedProxy);
            _disconnectedEvent.RemoveEventHandler(_vendorInstance, _disconnectedProxy);
        }
        catch { /* best effort */ }

        _alc.Unload();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
