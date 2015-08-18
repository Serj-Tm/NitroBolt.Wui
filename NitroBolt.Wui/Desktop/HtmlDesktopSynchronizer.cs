using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Windows.Forms;

namespace NitroBolt.Wui
{
  public class HtmlDesktopSynchronizer:XHDesktopSynchronizer<XElement>
  {
    public HtmlDesktopSynchronizer(WebBrowser webBrowser, Func<XElement> htmlView, Action<JsonData> eventer, TimeSpan? refreshInterval = null, bool isTrace = false):
      base(webBrowser, htmlView, eventer, refreshInterval, isTrace)
    {
    }
    public override XElement MakeStartPage(XElement page)
    {
      return new XElement("html", page?.Attributes(), XRaw.Create(page.Element("head")?.ToString()), new XElement("body"));
    }
    public override IEnumerable<object> GetSyncCommands(XElement page)
    {
      return HtmlJavaScriptDiffer.JsSync(new XElementProvider(), lastPage.Element("body"), page.Element("body"));
    }

    public static XElement[] Scripts(bool isDebug=false)
    {
      var scripts = HtmlJavaScriptDiffer.Scripts(new XElementProvider(), isDebug);
      return scripts.Where(element => element.Name == "meta")
        .Concat(
          new[]
            {
             new XElement("script",
                new XAttribute("type", "text/javascript"),
                new XRaw(
                  @"
                    function server_event(json)
                    {
                      window.external.Event(json);
                    }
                  "
                )
              )
            }
      )
      .Concat(scripts.Where(element => element.Name != "meta"))
      .ToArray();
    }

  }

  public class XRaw : XText
  {
    public XRaw(string text) : base(text) { }
    public XRaw(XText text) : base(text) { }

    public override void WriteTo(System.Xml.XmlWriter writer)
    {
      writer.WriteRaw(this.Value);
    }
    public static XRaw Create(string text)
    {
      if (text == null)
        return null;
      return new XRaw(text);
    }
  }

}
