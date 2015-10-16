using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.XPath;
using System.Xml.Linq;
using NitroBolt.Wui;
using NitroBolt.Functional;

namespace NitroBolt.WebSampler
{
    public class Main
    {
        public static NitroBolt.Wui.HtmlResult<HElement> HView(object _state, JsonData[] jsons, HContext context)
        {
            var state = _state.As<MainState>() ?? new MainState();

            foreach (var json in jsons.OrEmpty())
            {

                switch (json.JPath("data", "command")?.ToString())
                {
                    case "text":
                        state = state.With(text: json.JPath("value")?.ToString());
                        System.Threading.Thread.Sleep(new Random().Next(0, 500));
                        break;
                    case "error":
                        throw new Exception("ошибка");
                    default:
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

            var page = h.Html
            (
              h.Head(
                h.Element("title", "NitroBolt.Wui.WebConsole")
              ),
              h.Body
              (
                h.Div
                (
                  h.Raw(string.Format("1<b>{0:dd.MM.yy.HH:mm:ss.fff}</b> 2", DateTime.Now))
                ),
                h.Input(h.type("text"), h.Attribute("onkeyup", ";"), new hdata { { "command", "text" } }),
                h.Input(h.type("button"), h.onclick(";"), new hdata { { "command", "error" } }, h.value("throw error")),
                h.Div(state.Text),
                h.Div(h.A(h.href("multi.html"), "multi sync frames")),
                h.Div(h.A(h.href("auth-view.html?id=12"), "auth-view"))
              //h.Div(DateTime.UtcNow),
              //h.Input(h.type("button"), h.onclick(";"), h.value("update")),
              //h.Div(1, h.Attribute("js-init", "$(this).css('color', 'red')"))
              )
            );
            return page;
        }



        static readonly HBuilder h = null;

    }
    class MainState
    {
        public MainState(string text = null)
        {
            this.Text = text;
        }
        public readonly string Text;
        public MainState With(Option<string> text = null)
        {
            return new MainState(text: text.Else(Text));
        }
    }

}