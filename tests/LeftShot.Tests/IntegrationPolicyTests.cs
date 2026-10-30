using System;
using System.Windows;
using LeftShot.Core;
using LeftShot.Services;
using NUnit.Framework;

namespace LeftShot.Tests;
[TestFixture]
public class IntegrationPolicyTests
{
    [TestCase(1)] [TestCase(1.25)] [TestCase(1.5)] [TestCase(2)]
    public void PreviewRemainsBottomLeftOfNegativeOriginWorkArea(double scale)
    {
        var work=new Rect(-2560,-1440,2560,1400);
        var placed=ResourcePolicy.PreviewBounds(work,scale,new Size(312,300));
        Assert.That(work.Contains(placed),Is.True);
        Assert.That(placed.Left,Is.EqualTo(work.Left+16*scale));
        Assert.That(placed.Bottom,Is.EqualTo(work.Bottom-16*scale));
    }
    [Test]
    public void PreviewFitsTinyWorkArea()
    {var work=new Rect(-100,100,240,200);Assert.That(work.Contains(ResourcePolicy.PreviewBounds(work,2,new Size(312,350))),Is.True);}
    [Test]
    public void BudgetAccountsForTransientCaptureBuffers()
    {Assert.That(ResourcePolicy.CanAllocate(128*1024*1024,32_000_000),Is.True);Assert.That(ResourcePolicy.CanAllocate(256*1024*1024,32_000_000),Is.False);Assert.That(ResourcePolicy.CanAllocate(0,long.MaxValue),Is.False);}
    [TestCase("Ctrl+F8",true)] [TestCase("Ctrl+Shift+F8",true)] [TestCase("Alt+F9",true)]
    [TestCase("Win+Shift+S",false)] [TestCase("PrintScreen",false)]
    [TestCase("Ctrl+Ctrl+F8",false)] [TestCase("Ctrl+Shift+A",true)]
    [TestCase("Ctrl+Alt+S",true)] [TestCase("ctrl+alt+s",true)]
    [TestCase("Ctrl+Alt+62",false)] [TestCase("Ctrl+Alt+Return",false)] [TestCase("S",false)]
    public void HotkeysAcceptModifiedLettersAndFunctionKeysWithoutReservedBindings(string binding,bool expected)
    {Assert.That(Hotkeys.TryParse(binding,out _,out _),Is.EqualTo(expected));}
    [Test] public void ScreenshotLetterBindingMapsToControlAltAndVirtualS()
    {Assert.That(Hotkeys.TryParse("Ctrl+Alt+S",out uint modifiers,out uint key),Is.True);Assert.That(modifiers,Is.EqualTo(3));Assert.That(key,Is.EqualTo(0x53));}
}
