using NitroBolt.Wui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.XPath;
using System.Xml.Linq;
using NitroBolt.Functional;

namespace NitroBolt.WebSampler
{
  public class Part1
  {
    public static NitroBolt.Wui.HtmlResult<HElement> HView(object _state, JsonData[] jsons, HContext context)
    {
      var state = _state.As<MainState>() ?? new MainState();

      foreach (var json in jsons.OrEmpty())
      {
        //var account = MemoryDatabase.World.Accounts.FirstOrDefault(_account => _account.Name == "darkgray");

        switch (json.JPath("data", "command")?.ToString())
        {
          default:
            //state = new MyLibState(new[] { new Message(json.ToString_Fair()) }.Concat(state.Messages).Take(10).ToArray());
            break;
        }
      }
     

      var page = Page(state);
      return new NitroBolt.Wui.HtmlResult<HElement>
      {
        Html = page,
        State = state,
      };
    }

   
    private static HElement Page(MainState state)
    {

      var page = h.Div
      (
        h.Div(DateTime.UtcNow),
        h.Input(h.type("button"), h.onclick(";"), h.value("update")),
        h.Div(1, h.Attribute("js-init", "$(this).css('color', 'red')"))
      );
      return page;
    }



    static readonly HBuilder h = null;

  }


}