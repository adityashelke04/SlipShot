using System.Threading;
using NUnit.Framework;
using SlipShot.Services;

namespace SlipShot.Tests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class ServiceAuditTests
{
    [TestCase("Ctrl+F1,F2")]
    [TestCase("Ctrl+F1, F1")]
    public void CombinedFunctionKeyNamesAreRejected(string binding)
    {
        Assert.That(Hotkeys.TryParse(binding, out _, out _), Is.False,
            "A binding must name exactly one function key, rather than silently combining enum values.");
    }

    [Test, Explicit("Attended acceptance only: temporarily registers a native Windows hotkey; PRODUCT.md prohibits hotkey registration during automatic verification.")]
    public void EquivalentHotkeySpellingKeepsTheExistingRegistration()
    {
        using var hotkeys = new Hotkeys();
        int function = 24;
        while (function >= 13 && !hotkeys.Register($"Ctrl+Alt+Shift+F{function}")) function--;
        if (function < 13) Assert.Ignore("No unused modified function key is available for the native registration check.");

        Assert.That(hotkeys.Register($"shift+ctrl+alt+f{function}"), Is.True,
            "Changing modifier order or letter case must not make the app conflict with its own hotkey.");
        Assert.That(hotkeys.Register($"Ctrl+Alt+Shift+F{function}"), Is.True);
    }
}
