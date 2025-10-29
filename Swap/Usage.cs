// Initial pick based on the communicator exe present on the client.
var proxy = new HotSwappableMobileService(
    MobileServiceFactory.CreateForCommunicator(@"C:\Program Files\Vendor\Comm\comm.exe"));

proxy.Connected += (_, e) =>
    Console.WriteLine($"Connected: device={e.DeviceId}, via={e.Transport}, reconnected={e.IsReconnected}");

proxy.Disconnected += (_, e) =>
    Console.WriteLine($"Disconnected: device={e.DeviceId}");

proxy.Connect();

var loc = proxy.GetLocation();
Console.WriteLine($"Location: {loc.Latitude}, {loc.Longitude} (±{loc.AccuracyMeters?.ToString() ?? "?"} m)");

// … later at runtime (communicator updated, user chooses a different stack, etc.)
proxy.SwapTo(() =>
    MobileServiceFactory.CreateForCommunicator(@"D:\Deployed\Comm\comm.exe"));

var loc2 = proxy.GetLocation();
Console.WriteLine($"Location (new DLL): {loc2.Latitude}, {loc2.Longitude}");

proxy.Disconnect();
proxy.Dispose();
