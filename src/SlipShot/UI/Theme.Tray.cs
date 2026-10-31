using System.Drawing;
using System.Windows;
using System.Windows.Media;
using Forms=System.Windows.Forms;

namespace SlipShot.UI;

internal static partial class Theme
{
    internal static void Apply(Forms.ContextMenuStrip menu)
    {
        menu.BackColor=DrawingColor("Panel");menu.ForeColor=DrawingColor("Ink");
        foreach(Forms.ToolStripItem item in menu.Items)item.ForeColor=menu.ForeColor;
        if(SystemParameters.HighContrast)menu.RenderMode=Forms.ToolStripRenderMode.System;
        else if(menu.Renderer is not TrayRenderer)menu.Renderer=new TrayRenderer();
    }

    private static System.Drawing.Color DrawingColor(string token)
    {
        var c=((SolidColorBrush)Get(token)).Color;
        return System.Drawing.Color.FromArgb(c.A,c.R,c.G,c.B);
    }

    private sealed class TrayRenderer : Forms.ToolStripProfessionalRenderer
    {
        internal TrayRenderer():base(new TrayColors()){RoundedEdges=false;}
        protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
        {e.TextColor=DrawingColor(e.Item.Enabled?"Ink":"Muted");base.OnRenderItemText(e);}
    }

    private sealed class TrayColors : Forms.ProfessionalColorTable
    {
        internal TrayColors(){UseSystemColors=false;}
        public override System.Drawing.Color ToolStripDropDownBackground=>DrawingColor("Panel");
        public override System.Drawing.Color ImageMarginGradientBegin=>DrawingColor("Panel");
        public override System.Drawing.Color ImageMarginGradientMiddle=>DrawingColor("Panel");
        public override System.Drawing.Color ImageMarginGradientEnd=>DrawingColor("Panel");
        public override System.Drawing.Color MenuBorder=>DrawingColor("Border");
        public override System.Drawing.Color MenuItemSelected=>DrawingColor("Selected");
        public override System.Drawing.Color MenuItemBorder=>DrawingColor("Focus");
        public override System.Drawing.Color MenuItemSelectedGradientBegin=>DrawingColor("Selected");
        public override System.Drawing.Color MenuItemSelectedGradientEnd=>DrawingColor("Selected");
        public override System.Drawing.Color SeparatorDark=>DrawingColor("Border");
        public override System.Drawing.Color SeparatorLight=>DrawingColor("Panel");
    }
}
