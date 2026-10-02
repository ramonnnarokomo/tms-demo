using System.Security.Cryptography;

namespace TmsDemo.Api.Domain;

/// <summary>Generates tracking numbers like TMS261002K7Q2MX (prefix + date + 6 random chars).</summary>
public static class TrackingNumberGenerator
{
    // No 0/O or 1/I to avoid confusion when someone reads the code over the phone.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Next(DateTime nowUtc) =>
        $"TMS{nowUtc:yyMMdd}{RandomNumberGenerator.GetString(Alphabet, 6)}";
}
