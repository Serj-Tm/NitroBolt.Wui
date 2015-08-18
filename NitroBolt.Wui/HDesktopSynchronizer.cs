using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NitroBolt.Wui
{
  public class HDesktopSynchronizer: XHDesktopSynchronizer<HElement>
  {
    public HDesktopSynchronizer(WebBrowser webBrowser, Func<HElement> htmlView, Action<JsonData> eventer, TimeSpan? refreshInterval = null, bool isTrace = false):
      base(webBrowser, htmlView, eventer, refreshInterval, isTrace)
    {
    }
    public override HElement MakeStartPage(HElement page)
    {
      return new HElement("html", page?.Attributes, page.Element("head"), new HElement("body"));
    }
    public override IEnumerable<object> GetSyncCommands(HElement page)
    {
      return HtmlJavaScriptSynchronizer.JsSync(new HElementProvider(), lastPage.Element("body"), page.Element("body"));
    }

    public static HElement[] Scripts(bool isDebug = false)
    {
      var scripts = HtmlJavaScriptSynchronizer.Scripts(new HElementProvider(), isDebug).Where(element => element != null);
      return scripts.Where(element => element.Name.LocalName == "meta")
        .Concat(
          new[]
            {
             new HElement("script",
                new HAttribute("type", "text/javascript"),
                new HRaw(
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
      .Concat(scripts.Where(element => element.Name.LocalName != "meta"))
      .ToArray();
    }

  }

}
