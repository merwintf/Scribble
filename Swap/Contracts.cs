using System;

public sealed class ConnectionArgs : EventArgs
{
    public string? DeviceId { get; }
    public string? Transport { get; }
    public bool IsReconnected { get; }

    public ConnectionArgs(string? deviceId, string? transport, bool isReconnected)
        => (DeviceId, Transport, IsReconnected) = (deviceId, transport, isReconnected);
}

public sealed class Location
{
    public double Latitude { get; }
    public double Longitude { get; }
    public double? AccuracyMeters { get; }

    public Location(double latitude, double longitude, double? accuracyMeters = null)
        => (Latitude, Longitude, AccuracyMeters) = (latitude, longitude, accuracyMeters);
}

public interface IMobileService
{
    event EventHandler<ConnectionArgs> Connected;
    event EventHandler<ConnectionArgs> Disconnected;

    void Connect();
    void Disconnect();
    Location GetLocation(); // wraps vendor’s GetLocationMethod/GetLocation/etc.
}
