using System;
using System.Diagnostics;

public static class MobileServiceFactory
{
    public static IMobileService CreateForCommunicator(string communicatorExePath)
    {
        var verStr = FileVersionInfo.GetVersionInfo(communicatorExePath).FileVersion ?? "0.0.0.0";
        var ver = new Version(verStr);

        // Example: pick folders/names by version. Adjust paths/type names to your vendor.
        if (ver.Major >= 3)
        {
            return new MobileServiceAdapter(
                dllDirectory: @"C:\Vendor\Mobile\v3",
                dllFileName: "Vendor.Mobile.v3.dll",
                typeFullName: "Vendor.Mobile.MobileService");
        }
        if (ver.Major == 2)
        {
            return new MobileServiceAdapter(
                dllDirectory: @"C:\Vendor\Mobile\v2",
                dllFileName: "Vendor.Mobile.v2.dll",
                typeFullName: "Vendor.Mobile.MobileService");
        }
        return new MobileServiceAdapter(
            dllDirectory: @"C:\Vendor\Mobile\v1",
            dllFileName: "Vendor.Mobile.v1.dll",
            typeFullName: "Vendor.Mobile.MobileServiceLegacy");
    }
}
