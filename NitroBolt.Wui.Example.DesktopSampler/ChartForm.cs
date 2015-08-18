using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using NitroBolt.Functional;

namespace NitroBolt.Wui
{
  public partial class ChartForm : Form
  {
    public ChartForm()
    {

      //if (true)//ставим поддержку IE11
      //{
      //  var appName = System.IO.Path.GetFileName(Application.ExecutablePath);

      //  Microsoft.Win32.Registry
      //    .CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true)
      //    .SetValue(appName, 11000, Microsoft.Win32.RegistryValueKind.DWord);
      //}

      //var avatar = CreateAvatar();
      //System.IO.File.WriteAllBytes(ApplicationHelper.MapPath("avatar.png"), avatar);

      h.Protocol().Register_ResourceProtocol(StarResource.ResourceManager);
      h.Protocol().Register("nres2", (url, context) => (byte[])StarResource.ResourceManager.GetObject(url.Substring(context.ProtocolPrefix.Length + 3).Trim('/', '\\').Replace('.', '_')));
      //h.Protocol().Register("avatar", (url, context) => avatar);

      InitializeComponent();

      var values = new List<PinValue<double>>();

      var timer = new Timer{ Interval = (int)TimeSpan.FromSeconds(0.15).TotalMilliseconds };
      timer.Tick += (_s, _e) =>
        values.Add(new PinValue<double>(DateTime.UtcNow, Math.Sin((DateTime.UtcNow - new DateTime(2000, 1, 1)).TotalSeconds)));
      timer.Start();


      //Action<object, EventArgs> action = (o, e) => Console.WriteLine(e);

      //foreach (var eventInfo in this.GetType().GetEvents())
      //{
      //  EventTest.AddEventHandler(eventInfo, this, action);
      //}
      //this.htmlSync = new HtmlDesktopSynchronizer(webBrowser1, () => HtmlView(), json => Console.WriteLine(json.ToString()), isTrace:false);
      this.htmlSync = new HDesktopSynchronizer(webBrowser1, () => HView(values), json => Console.WriteLine(json.ToString()), isTrace: false, refreshInterval: TimeSpan.FromMilliseconds(100));
      webBrowser1.ScriptErrorsSuppressed = true;
      webBrowser1.DocumentCompleted += (_s, _e) =>
        {
          webBrowser1.Document.Window.Error += (__s, __e) =>
            Console.WriteLine("{0}: {1}[{2}]", __e.Description, __e.Url, __e.LineNumber);
        };
    }
    //Гипотрохоида
    static byte[] CreateAvatar()
    {
      var bmp = new Bitmap(80, 80);
      using (var g = Graphics.FromImage(bmp))
      {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
        g.Clear(Color.FromArgb(unchecked((int)0x00000000)));
        //g.DrawRectangle(Pens.Blue, 10, 10, 20, 20);
        var cx = 40;
        var cy = 40;
        for (var i = 0; i < 1010; ++i)
        {
          //var r = 29;
          var r = 29.31;
          var Rr = 11;
          var p = Hypotrochoid(i * 0.05, Rr, r, 12.5);
          var l = Math.Sqrt(p.X * p.X + p.Y * p.Y);

          var af = 0.0 + 0.5 * l / cx;

          g.FillEllipse(new SolidBrush(Color.Black.With(af:af)), p.X * 1.6f + cx, p.Y * 1.6f + cy, 1.6f, 1.6f);
        }
      }
      using (var stream = new System.IO.MemoryStream())
      {
        bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
      }
    }
    static PointF Hypotrochoid(double phi, double Rr, double r, double h)
    {
      //var Rr = R - r;
      var x = Rr * Math.Cos(phi) + h * Math.Cos(Rr/r * phi);
      var y = Rr * Math.Sin(phi) + h * Math.Sin(Rr/r * phi);
      return new PointF((float)x, (float)y);
    }


    class PinValue<T>:IPinValue
    {
      public PinValue(DateTime time, T value)
      {
        this.Time = time;
        this.Value = value;
      }

      public readonly DateTime Time;
      public readonly T Value;

      DateTime IPinValue.Time
      {
        get { return this.Time; }
      }
      object IPinValue.Value
      {
        get { return this.Value; }
      }
    }
    interface IPinValue
    {
      DateTime Time {get;}
      object Value { get; }
    }

    //HtmlDesktopSynchronizer htmlSync;
    HDesktopSynchronizer htmlSync;

    static IEnumerable<PointF> F(double start, double end, double dx)
    {
      for (var x = start; x <= end; x += dx)
        yield return new PointF((float)x, (float)Math.Sin(x));
    }
    static IEnumerable<PointF> Shift(IEnumerable<PointF> points, double dx, double dy)
    {
      return points.Select(p => new PointF((float)(p.X + dx), (float)(p.Y + dy)));
    }

    HElement HView(List<PinValue<double>> values)
    {
      var s = DateTime.Now.Minute * 60 +  DateTime.Now.Second + DateTime.Now.Millisecond / 1000.0;

      var svgNs = "http://www.w3.org/2000/svg";

      return new HElement("html",
        new HElement("head",
          HDesktopSynchronizer.Scripts(),
          new HElement("link", 
            new HAttribute("type", "text/css"), 
            new HAttribute("rel", "stylesheet"), 
            new HAttribute("href", "http://cdnjs.cloudflare.com/ajax/libs/rickshaw/1.3.0/rickshaw.css")
          ),
          new HElement("script", new HAttribute("src", "http://cdnjs.cloudflare.com/ajax/libs/d3/3.2.2/d3.js"), ""),
          new HElement("script", new HAttribute("src", "http://cdnjs.cloudflare.com/ajax/libs/rickshaw/1.3.0/rickshaw.js"), ""),
          h.Css(
            @"
            h1 {font-size:100%;}
            "
          )
        ),
        new HElement("body",
          h.Div(
            h.Element("h1", "click"),
            h.style("display:inline-block;"),
            new HElement("div", new HAttribute("onclick", ";"), new HAttribute("data-command", "tt"), "onclick")
          ),
          h.Div(
            h.style("display:inline-block;margin:10px;"),
            h.Element("h1", "использование js-init"),
            new HElement("div",
              new HAttribute("js-update", "$(_this).html(new Date())"),
              11
            )
          ),
          h.Div
          (
            h.Element("h1", "container"),
            h.Div(
              h.data("name", "book"),
              h.data("book-id", "1"),
              h.Input(h.type("text"), h.data("name", "author"), h.value("Голдратт")),
              h.Input(h.type("text"), h.data("name", "title"), h.value("Цель")),
              h.Input(h.type("checkbox"), h.@checked(), h.data("name", "is-readed")),
              h.Select(h.data("name", "category"),
                h.Option(h.value(""), "-"),
                h.Option(h.value("managment"), "управление"),
                h.Option(h.value("sci-fi"), "фантастика"),
                h.Option(h.value("detective"), "детектив")
              ),
              h.Input(h.type("button"), h.onclick(";"), h.data("container", "book"), h.data("command", "new-book"), h.value("добавить"))
            ),
            h.Div(h.style("font-size:85%;color:#505050;"), "<div data-name='bla-bla'><input type='button' data-container='bla-bla'/><input type='text' data-name='age' /> </div>")
          ),
          h.Div(h.Element("h1", "drag & drop"),
            new HElement("div",
              new HAttribute("style", "border:1px solid black;width:50px;height:50px;padding:10px;display:inline-block;"),
              new HAttribute("js-init", "$(_this).draggable({ opacity: 0.8, helper: 'clone'  });"),
              new HAttribute("data-r", 1),
              "draggable"
            ),
            new HElement("div",
              new HAttribute("style", "border:1px solid black;width:100px;height:100px;padding:10px;display:inline-block;"),
              new HAttribute("js-init", "$(_this).droppable({drop: function() {server_element_event(this, 'drop');}  });"),
              new HAttribute("data-s", 1),
        //new HAttribute("onclick", "alert(12)"),
              "droppable"
            ),
            h.Span("todo: добавить свойство, которое указывает какие data-свойства не надо передавать")
          ),
          //h.Div
          //(
          //  h.Span(((DateTime.UtcNow.Second / 2) % 2 == 0) ? (object)"T" : h.Span("t")), 
          //  h.Span(" - "),
          //  h.Span((DateTime.UtcNow.Second / 2) % 2)
          //)//,
          h.Div
          (
            h.style("display:inline-block;"),
            h.Element("h1", "svg"),
            h.Element(new HName(svgNs, "svg"),
              h.Element(new HName(svgNs, "circle"), h.Attribute("cx", 100), h.Attribute("cy", 94), h.Attribute("r", 10 + DateTime.UtcNow.Second)),
              h.Element(new HName(svgNs, "polygon"), h.Attribute("points", "100,10 40,180 190,60 10,60 160,180"), h.style("fill:orange;stroke:purple;stroke-width:5;fill-rule:evenodd;"))
            )
          ),
          h.Div
          (
            h.style("display:inline-block;"),
            h.Element("h1", "Изображения из ресурсов"),
            new HElement("image", new HAttribute("src", @"nres://gold_star.jpg"), new HAttribute("style", "width:100px")),
            new HElement("image", new HAttribute("src", @"nres://gold_star2.jpg"), new HAttribute("style", "width:100px")),
            new HElement("image", new HAttribute("src", @"nres://gold_metal_star.jpg"), new HAttribute("style", "width:100px")),
            new HElement("image", new HAttribute("src", @"nres2://gold_metal_star.jpg"), new HAttribute("style", "width:100px")),
            new HElement("image", new HAttribute("src", @"nres2://not-found.jpg"), new HAttribute("style", "width:100px"), h.title("проверка работы отсутствующего изображения"))
          ),
          h.Element("h1", "chart"),
          new HElement("p",
            new HAttribute("style", "padding:40px;margin:20px;border:1px solid black;"),
            new HAttribute("js-init",
@"
var palette = new Rickshaw.Color.Palette( { scheme: 'spectrum14' } );

var graph = new Rickshaw.Graph( {
	element: $(_this)[0],
	width: 900,
	height: 500,
	renderer: 'line',
	stroke: true,
	preserve: true,
	series: [
		{
			color: palette.color(),
			data: [{x:1, y:1}],
			name: 'Moscow'
		},
//		{
//			color: palette.color(),
//			data: [{x:1, y:1}],
//			name: 'Moscow'
//		},
//    {
//			color: palette.color(),
//			data: [{x:1, y:1}],
//			name: 'Shanghai'
//		}, 
	]
} );

graph.render();

jQuery.data(_this[0], 'graph', graph);
jQuery.data(_this[0], 'x1', '15');


"),
 new HAttribute("js-update", @"
var _graph = jQuery.data(_this[0], 'graph');
//_this.css('background-color', 'red');
//alert(jQuery.data(_this[0], 'x1'));
//alert(_this[0]);
  _graph.series[0].data = [__data__];
  _graph.render();

".Replace("__data__",
        //Shift(F(s, s + 14, 0.3), -s, 4).Select(p => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{{x:{0}, y:{1}}}", p.X, p.Y)).JoinToString(", ")
  values.Select(p => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{{x:{0}, y:{1}}}", (p.Time - new DateTime(2013, 1, 1)).TotalHours, p.Value)).JoinToString(", ")
 )
        //new HAttribute("style", ((DateTime.Now.Second / 3) % 2) == 0 ? "background-color:green": "background-color:red")
            )
          )
        )
      );
    }
    static readonly HBuilder h = null;
//    XElement HtmlView()
//    {
//      return new XElement("html",
//        new XElement("head",
//          new XElement("meta",
//            new XAttribute("http-equiv", "X-UA-Compatible"),
//             new XAttribute("content", "IE=9")
//           ),
//          new XElement("link", 
//            new XAttribute("type", "text/css"), 
//            new XAttribute("rel", "stylesheet"), 
//            new XAttribute("href", "http://cdnjs.cloudflare.com/ajax/libs/rickshaw/1.3.0/rickshaw.css")
//          ),
//          HtmlDesktopSynchronizer.Scripts(),       
//          new XElement("script", new XAttribute("src", "http://cdnjs.cloudflare.com/ajax/libs/d3/3.2.2/d3.js"), ""),
//          new XElement("script", new XAttribute("src", "http://cdnjs.cloudflare.com/ajax/libs/rickshaw/1.3.0/rickshaw.js"), "")
//        ),
//        new XElement("body",
//          new XElement("div", new XAttribute("onclick", ";"), new XAttribute("data-command", "tt"), "r"), 
//          new XElement("div", 
//            new XAttribute("style", "border:1px solid black;width:50px;height:50px;padding:10px;"),
//            new XAttribute("js-init", "$(_this).draggable({ opacity: 0.8, helper: 'clone'  });"),
//            new XAttribute("data-r", 1),
//            "test"
//          ),
//          new XElement("div",
//            new XAttribute("style", "border:1px solid black;width:100px;height:100px;padding:10px;"),
//            new XAttribute("js-init", "$(_this).droppable({drop: function() {server_element_event(this, 'drop');}  });"),
//            new XAttribute("data-s", 1),
//            "storage"
//          ),
//          new XElement("div", 
//            new XAttribute("js-init",
//@"
//var palette = new Rickshaw.Color.Palette( { scheme: 'classic9' } );
//
//var graph = new Rickshaw.Graph( {
//	element: $(_this)[0],
//	width: 900,
//	height: 500,
//	renderer: 'line',
//	stroke: true,
//	preserve: true,
//	series: [
//		{
//			color: palette.color(),
//			data: [{x:0, y:4}, {x:1, y:5}, {x:2, y:1}, {x:4, y:7}, {x:5, y:8}],
//			name: 'Moscow'
//		}, {
//			color: palette.color(),
//			data: [{x:0, y:19}, {x:1, y:15}, {x:2, y:10}, {x:4, y:3}, {x:5, y:5}],
//			name: 'Shanghai'
//		}, 
//	]
//} );
//
//graph.render();
//
//")
            
//          )
//        )
//      );
//    }
  }
  public static class ColorHlp
  {
    public static Color With(this Color color, double? af = null)
    {
      var a = color.A;
      if (af != null)
      {
        if (af < 0)
          a = 0;
        else if (af >= 1)
          af = 255;
        else
          a = (byte)(255 * af);
      }
      return Color.FromArgb(a, color.R, color.G, color.B);
    }
  }
}
