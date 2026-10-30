using System;
using System.Windows;

namespace SlipShot.Core;
public sealed class PreviewGesture
{
    private Point start;
    public bool Holding {get;private set;}
    public bool Dragging {get;private set;}
    public void Press(Point point) {start=point;Holding=true;Dragging=false;}
    public bool Move(Point point,double thresholdX,double thresholdY)
    {
        if(!Holding||Dragging)return false;
        if(Math.Abs(point.X-start.X)<thresholdX&&Math.Abs(point.Y-start.Y)<thresholdY)return false;
        Dragging=true;return true;
    }
    public bool Release() {bool click=Holding&&!Dragging;Holding=false;Dragging=false;return click;}
    public void Cancel() {Holding=false;Dragging=false;}
}
