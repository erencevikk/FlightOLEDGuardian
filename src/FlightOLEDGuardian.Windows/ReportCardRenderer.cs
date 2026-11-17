using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FlightOLEDGuardian.Reporting;

namespace FlightOLEDGuardian.Windows;

public static class ReportCardRenderer
{
    public static void Export(FlightReport report,int width,int height,string path,string language="en")
    {
        if((width,height) is not ((1920,1080) or (1080,1920) or (1080,1080)))throw new ArgumentException("Unsupported card dimensions");
        var visual=new DrawingVisual();
        using(var draw=visual.RenderOpen())
        {
            string T(string text)=>UiText.Get(text,language);
            var accent=new SolidColorBrush(Color.FromRgb(69,219,204));
            var muted=new SolidColorBrush(Color.FromRgb(143,165,187));
            var white=new SolidColorBrush(Color.FromRgb(228,237,246));
            draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(12,18,27)),null,new Rect(0,0,width,height));
            double margin=72,bodyWidth=width-margin*2;
            void Text(string text,double x,double y,double size,Brush brush,double? maxWidth=null)
            {
                var formatted=new FormattedText(text,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),size,brush,1){MaxTextWidth=maxWidth??bodyWidth,Trimming=TextTrimming.CharacterEllipsis};
                draw.DrawText(formatted,new Point(x,y));
            }
            Text("FLIGHTOLED GUARDIAN",margin,60,32,accent);
            Text(T("FLIGHT COMPLETED")+(report.IsMock?" • "+T("MOCK / TEST DATA"):""),margin,140,24,muted);
            Text(report.Aircraft,margin,195,width==1920?58:48,white);
            Text(report.TouchdownUtc.ToLocalTime().ToString("dd MMM yyyy · HH:mm",CultureInfo.GetCultureInfo(language=="tr"?"tr-TR":"en-US")),margin,275,26,muted);
            bool portrait=height>width;
            double top=380,footer=height-150, gap=24;
            int columns=portrait?1:2;
            double cellWidth=(bodyWidth-(columns-1)*gap)/columns;
            double cellHeight=portrait?185:150;
            (string label,string value)[] metrics=[
                ("FLIGHT TIME",TimeSpan.FromSeconds(report.FlightSeconds).ToString(@"hh\:mm\:ss")),
                ("OLED BLACK TIME",TimeSpan.FromSeconds(report.BlackSeconds).ToString(@"hh\:mm\:ss")),
                ("ELIGIBLE TIME",TimeSpan.FromSeconds(report.EligibleSeconds).ToString(@"hh\:mm\:ss")),
                ("COMPLETED CYCLES",report.Cycles.ToString(CultureInfo.InvariantCulture)),
                ("LONGEST BLACK INTERVAL",$"{report.LongestBlackSeconds:0} SEC"),
                ("LANDING RATE · SAMPLED",report.LandingRate is {} rate?$"{rate:0} FPM":"Unavailable")];
            for(int i=0;i<metrics.Length;i++)
            {
                int row=i/columns,col=i%columns;
                double x=margin+col*(cellWidth+gap),y=top+row*(cellHeight+gap);
                draw.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(20,31,45)),null,new Rect(x,y,cellWidth,cellHeight),16,16);
                Text(T(metrics[i].label),x+26,y+22,20,muted,cellWidth-52);
                Text(metrics[i].value,x+26,y+65,42,i==1?accent:white,cellWidth-52);
            }
            draw.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(40,60,76)),2),new Point(margin,footer),new Point(width-margin,footer));
            Text(T("Protected with FlightOLED Guardian"),margin,footer+25,25,accent);
            Text(T("Measured display black time. No burn-in guarantee."),margin,footer+70,20,muted);
        }
        var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(path);png.Save(file);
    }
}
