using NitroBolt.Wui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.XPath;
using System.Xml.Linq;
using NitroBolt.Functional;
using System.Net.Http;
using System.Web.Http;

namespace NitroBolt.WebSampler
{
    public class Part2Controller : ApiController
    {
        [HttpGet, HttpPost]
        [Route("part2")]
        public HttpResponseMessage Route()
        {
            return HWebApiSynchronizeHandler.Process<object>(this.Request, HView);
        }

        public static NitroBolt.Wui.HtmlResult<HElement> HView(object _state, JsonData[] jsons, HttpRequestMessage request)
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


            var page = Page(state, request);
            return new NitroBolt.Wui.HtmlResult<HElement>
            {
                Html = page,
                State = state,
            };
        }


        private static HElement Page(MainState state, HttpRequestMessage request)
        {

            var page = h.Div
            (
              h.Div(DateTime.UtcNow),
              h.Div(DateTime.UtcNow.Second / 2 % 2 == 0),
              h.Input(h.type("button"), h.onclick(";"), h.value("update")),
              h.Input(h.type("button"), h.onclick(";;sync.server_element_event(this, e)"), h.value("manual update"), new hdata { { "command", "test" } }),
              h.Div(1, h.Attribute("js-init", "$(this).css('color', 'green')")),
              h.Div(DateTime.UtcNow.Second / 2 % 2 == 0 ? new HRaw("12<b>a</b>q") : null),
              h.Div
              (
                HWebApiSynchronizeHandler.Frames
                 .Select(frame => h.Div
                  (
                     h.Div($"{frame.Key}: {frame.Value.Cycle}"),
                     frame.Value.Updates.Select(update => h.Div(h.style("margin-left:10px"), $"{update.Key}: {update.Value.Elapsed}"))
                  )
                  )
              ),
              h.Div
              (
                  h.Raw($"121<b>{DateTime.UtcNow.Ticks}</b>12")
              )
            );
            return page;
        }



        static readonly HBuilder h = null;

    }


}