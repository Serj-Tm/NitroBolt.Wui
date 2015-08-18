using NitroBolt.Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NitroBolt.Wui
{
  public class FcgiSynchronizer
  {
    public static HElement[] Scripts(string handlerName, int cycle, bool isDebug = false)
    {
      return new[]
        {
          h.Script(
            h.type("text/javascript"),
            new HRaw(
            @"
            function server_event(json)
            {
              $.post('__HandlerName__.js?cycle=' + cycle, { 'cycle': cycle, 'values':json },
                function(data)
                {
                  if (data.prev_cycle == cycle)
                  {
                    sync_page(data.commands);
                    cycle = data.cycle;
                  }
                }, 'json');
            }
            ".Replace("__HandlerName__", handlerName)
            )
          )
        }
        .Concat(NitroBolt.Wui.HtmlJavaScriptSynchronizer.Scripts(new HElementProvider(), isDebug: isDebug))
        .Concat(new[]
        {
          h.Script(
            h.type("text/javascript"),
            new HRaw(string.Format("var cycle = {0};", cycle))
          ),
          h.Script(
            h.type("text/javascript"),
            new HRaw(
            @"
              function update_page()
              {
                  try
                  {
                    $.getJSON('__HandlerName__.js?cycle=' + cycle + '&r=' + (1000 * Math.random() + '').substring(0,3), function(data) 
                     {
                        if (data.prev_cycle == cycle)
                        {
                          sync_page(data.commands);
                          cycle = data.cycle;
                        }
                     });
                  }
                  catch(e) {}
              }
              window.setInterval(update_page, 2000);
              update_page();
            ".Replace("__HandlerName__", handlerName)
            )
          )
        })
        .ToArray();
    }

    public FcgiSynchronizer(Dictionary<string, Func<string, int, JsonData, HtmlResult<HElement>>> handlers)
    {
      this.Handlers = handlers;
    }
    public readonly Dictionary<string, Func<string, int, JsonData, HtmlResult<HElement>>> Handlers;

    public string ProcessRequest(Dictionary<string, string> parameters, JsonData json)
    {

      var uri = parameters.Find("SCRIPT_NAME") + parameters.Find("PATH_INFO");
      var queryString = parameters.Find("QUERY_STRING");

      Console.WriteLine("{0} {1}, {2}", uri, queryString, Handlers.Select(pair => pair.Key).FirstOrDefault());

      var handlerPair = Handlers.FirstOrDefault(pair => uri?.StartsWith(pair.Key) ?? false);
      if (handlerPair.Value == null)
      {

        return "<html><body>invalid page</body></html>";
      }
      var handler = handlerPair.Value;
      var pageName = handlerPair.Key;

      if (uri.EndsWith(".js"))
      {
        var query = queryString.OrEmpty().Split('&').Select(pair => pair.Split('='))
          .ToDictionary(ss => ss.FirstOrDefault(), ss => ss.ElementAtOrDefault(1));
        var prevCycle = int.Parse(query.Find("cycle"));
        var prevUpdate = Updates.FirstOrDefault(_update => _update.Cycle == prevCycle);
        var prevPage = prevUpdate != null ? prevUpdate.Page : null;

        var update = PushUpdate(null);

        var result = handler(pageName, update.Cycle, json);
        //lock (locker)
        //{
        //  update.Page = result.Html;

        //}
        var page = result.Html;


        var commands = HtmlJavaScriptSynchronizer.JsSync(new HElementProvider(), prevPage == null ? null : prevPage.Element("body"), page.Element("body")).ToArray();
        var jupdate = new Dictionary<string, object>() { { "cycle", update.Cycle }, { "prev_cycle", prevCycle }, { "commands", commands } };

        var jsSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
        return jsSerializer.Serialize(jupdate);
      }
      else
      {
        var update = PushUpdate(null);

        var result = handler(pageName, update.Cycle, json);
        var page = result.Html;
        if (!uri.EndsWith(".raw.html"))
          page = h.Html(new HRaw(page.Element("head")?.ToString()), h.Body());

        //lock (locker)
        //{
        //  update.Page = page;
        //}
        return page.ToString();
      }
    }

    static UpdateCycle<HElement>[] Updates = new UpdateCycle<HElement>[] { };
    static readonly object locker = new object();
    static UpdateCycle<HElement> PushUpdate(HElement page)
    {
      lock (locker)
      {
        var lastUpdate = Updates.LastOrDefault();

        var cycle = (lastUpdate != null ? lastUpdate.Cycle : 0) + 1;

        var update = new UpdateCycle<HElement>(null, cycle, page, null);

        Updates = Updates.Skip(Math.Max(Updates.Length - 100, 0)).Concat(new[] { update }).ToArray();

        return update;
      }
    }
    static readonly HBuilder h = null;
  }


}
