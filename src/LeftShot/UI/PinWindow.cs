using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LeftShot.Services;

namespace LeftShot.UI;
internal sealed class PinWindow : Window
{
    public long ImageBytes { get; }
    internal PinWindow(BitmapSource bitmap)
    {
        Title="LeftShot · Pinned reference";Width=360;Height=280;MinWidth=160;MinHeight=120;
        ShowActivated=false;Topmost=true;ShowInTaskbar=false;
        Theme.Apply(this); ImageBytes=(long)bitmap.PixelWidth*bitmap.PixelHeight*4;
        var image=new Image { Source=bitmap,Stretch=Stretch.Uniform,Margin=new Thickness(8) };
        Content=image;
        SourceInitialized+=(_,_)=>Native.Protect(this,true);
        Closed+=(_,_)=>image.Source=null;
    }
}
