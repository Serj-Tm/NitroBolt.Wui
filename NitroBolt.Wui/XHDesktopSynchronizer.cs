using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Windows.Forms;

namespace NitroBolt.Wui
{
  public abstract class XHDesktopSynchronizer<TElement>
  {
    public XHDesktopSynchronizer(WebBrowser webBrowser, Func<TElement> htmlView, Action<JsonData> eventer, TimeSpan? refreshInterval = null, bool isTrace = false)
    {
      this.WebBrowser = webBrowser;
      this.htmlView = htmlView;
      this.IsTrace = isTrace;

      if (webBrowser != null)
      {
        //if (true)//ставим поддержку IE11
        //{
        //  var appName = System.IO.Path.GetFileName(Application.ExecutablePath);

        //  Microsoft.Win32.Registry
        //    .CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true)
        //    .SetValue(appName, 11000, Microsoft.Win32.RegistryValueKind.DWord);
        //}

        this.WebBrowser.ObjectForScripting = new Scripting(json => { if (eventer != null)eventer(json);this.WebBrowser.BeginInvoke(new Action(()=>ForceSynchronizer())); });
        this.Timer = new Timer() { Interval = (int)(refreshInterval ?? TimeSpan.FromSeconds(0.7)).TotalMilliseconds, Enabled = true };
        this.Timer.Tick += (_s, _e) => Synchronize(htmlView());
        this.WebBrowser.Disposed += (_s, _e) =>
        {
          if (this.Timer != null)
          {
            this.Timer.Dispose();
            this.Timer = null;
          }
        };
        //Console.WriteLine("sync[2]() {0}", this.GetHashCode());
        //this.WebBrowser.BeginInvoke(new Action(() => Synchronize(htmlView())));
        if (true)
        {
          var startTimer = new Timer { Interval = (int)TimeSpan.FromSeconds(0.1).TotalMilliseconds, Enabled = true };
          startTimer.Tick += (_s, _e) =>
          {
            //Console.WriteLine("start sync {0}", this.GetHashCode()); 
            Synchronize(htmlView());
            startTimer.Stop();
          };
        }
      }
    }
    public bool IsTrace = false;
    public readonly WebBrowser WebBrowser;
    public Timer Timer { get; private set; }
    protected Func<TElement> htmlView;

    protected TElement lastPage;
    public void ForceSynchronizer()
    {
      Synchronize(htmlView());
    }
    public void Synchronize(TElement page)
    {
      if (lastPage == null)
      {
        //Console.WriteLine("null sync {0}", this.GetHashCode());
        var null_page = MakeStartPage(page);
        WebBrowser.DocumentText = null_page.ToString();
        lastPage = null_page;
        //if (IsTrace)
        //{
        //  Console.WriteLine(null_page);
        //  Console.WriteLine(page);
        //}
        WebBrowser.DocumentCompleted += (_s, _e) => JsonSync(page);
        //{ Console.WriteLine("complete sync {0}", this.GetHashCode()); JsonSync(page); };
      }
      JsonSync(page);
    }

    public abstract TElement MakeStartPage(TElement page);
    public abstract IEnumerable<object> GetSyncCommands(TElement page);

    void JsonSync(TElement page)
    {
      if (!WebBrowser.IsBusy)
      {
        //Console.WriteLine("json sync {0}", this.GetHashCode());

        var commands = GetSyncCommands(page).ToArray();

        var jsSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
        var json = jsSerializer.Serialize(commands);
        if (IsTrace)
        {
          //Console.WriteLine(page.ToString_Fair());
          Console.WriteLine(JsonDataHlp.JsonObjectToString(jsSerializer.DeserializeObject(json)));
        }

        var res = WebBrowser.Document.InvokeScript("sync_page_from_json", new object[] { json });
        if (res != null)
          lastPage = page;
      }

    }

//    public static XElement[] Scripts(bool isDebug = false)
//    {
//      var scripts = HtmlJavaScriptSynchronizer.Scripts(isDebug);
//      return scripts.Where(element => element.Name == "meta")
//        .Concat(
//          new[]
//            {
//             new XElement("script",
//                new XAttribute("type", "text/javascript"),
//                new XRaw(
//                  @"
//                    function server_event(json)
//                    {
//                      window.external.Event(json);
//                    }
//                  "
//                )
//              )
//            }
//      )
//      .Concat(scripts.Where(element => element.Name != "meta"))
//      .ToArray();
//    }

  }


  [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
  [System.Runtime.InteropServices.ComVisible(true)]
  public class Scripting
  {
    public Scripting(Action<JsonData> eventer)
    {
      this.Eventer = eventer;
    }
    public readonly Action<JsonData> Eventer;
    public void Debug(object arg)
    {
      Console.WriteLine("{0}: {1}", arg, arg?.GetType());
    }
    public void Event(string json)
    {
      var jsSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
      if (Eventer != null)
        Eventer(new JsonData(jsSerializer.DeserializeObject(json)));
    }
  }
}
