﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.XPath;
using System.Xml.Linq;
using NitroBolt.Wui;
using NitroBolt.Functional;
using System.Net.Http;
using System.Web.Http;

namespace NitroBolt.WebSampler
{
    public class MainController:ApiController
    {
        [HttpGet, HttpPost]
        [Route("")]
        public HttpResponseMessage Route()
        {
            var response = HWebApiSynchronizeHandler.Process<object>(this.Request, HView);

            var cookie = new System.Net.Http.Headers.CookieHeaderValue("tick-id", DateTime.UtcNow.Ticks.ToString());
            cookie.Expires = DateTimeOffset.Now.AddMinutes(10);

            response.Headers.AddCookies(new[] { cookie });
            return response;

        }

        [HttpGet, HttpPost]
        [Route("main-raw")]
        public HttpResponseMessage RouteRaw()
        {
            return HWebApiSynchronizeHandler.Process<object>(this.Request, HViewRaw);

        }

        static HtmlResult HViewRaw(object _state, JsonData[] jsons, HttpRequestMessage request)
        {
            var result = HView(_state, jsons, request);
            result.FirstHtmlTransformer = html => html;
            return result;
        }

        public static HtmlResult HView(object _state, JsonData[] jsons, HttpRequestMessage request)
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
                    case "link":
                        state = state.With(text: "self:" + new Random().Next(1000));
                        break;
                    case "error":
                        throw new Exception("ошибка");
                    case "x":
                        state = state.With(text: json.JPath("value")?.ToString());
                        break;
                    case "x-container":
                        state = state.With(text: json.JPath("data", "x")?.ToString());
                        break;
                    case "array-name":
                        state = state.With(arrayNameResult: json.JPath("data", "x")?.ToString());
                        break;
                    case "two-question-mark":
                        state = state.With(text: json.JPath("data", "text")?.ToString());
                        break;
                    default:
                        break;
                }
            }

            var prevTickId = request.Headers.GetCookies("tick-id").FirstOrDefault()?.Cookies.FirstOrDefault()?.Value;


            var page = Page(state, prevTickId);
            return new HtmlResult
            {
                Html = page,
                State = state,
            };
        }


        private static HElement Page(MainState state, string prevTickId)
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
                h.Div("Previous request tick id(from cookie): " + prevTickId),
                h.Input(h.type("text"), h.Attribute("onkeyup", ";"), new hdata { { "command", "text" } }),
                h.Input(h.type("button"), h.onclick(";"), new hdata { { "command", "error" } }, h.value("throw error")),
                h.Div(state.Text),
                h.Div(h.A(h.href("./"), "main"), h.Span("("), h.A(h.href("main-raw"), "raw"), h.Span(")"), h.A(h.href("#"), "self-link", h.onclick("e.stopPropagation();"), h.data("command", "link"))),
                h.Div(h.A(h.href("multi.html"), "multi sync frames")),
                h.Div(h.A(h.href("auth?id=12"), "auth-view")),
              //h.Div(DateTime.UtcNow),
              //h.Input(h.type("button"), h.onclick(";"), h.value("update")),
              //h.Div(1, h.Attribute("js-init", "$(this).css('color', 'red')"))
                h.Div
                (
                   h.Input(h.type("radio"), h.Attribute("name", "x"), h.value("v1"), h.onclick(";"), new hdata { { "command", "x" } }), h.Span("V1"),
                   h.Input(h.type("radio"), h.Attribute("name", "x"), h.value("v2"), h.onclick(";"), h.@checked(), new hdata { { "command", "x" } }), h.Span("V2"),
                   h.Input(h.type("radio"), h.Attribute("name", "x"), h.value("v3"), h.onclick(";"), new hdata { { "command", "x" } }), h.Span("V3")
                ),
                h.Div
                (
                    h.data("name", "radio-container"),
                   h.Div("radio + container"),
                   h.Input(h.type("radio"), h.Attribute("name", "x1"), h.data("name", "x"), h.value("v1")), h.Span("V1"),
                   h.Input(h.type("radio"), h.Attribute("name", "x1"), h.data("name", "x"), h.value("v2"), h.@checked()), h.Span("V2"),
                   h.Input(h.type("radio"), h.Attribute("name", "x1"), h.data("name", "x"), h.value("v3")), h.Span("V3"),
                   h.Input(h.data("name", "t"), h.type("text"), h.value("tt")),
                   h.Input(h.type("button"), h.value("send"), h.onclick(";"), new hdata { { "command", "x-container" }, { "container", "radio-container" } })
                ),
                h.Div
                (
                    h.data("name", "array-name-container"),
                    h.Div("array name"),
                    h.Div(h.Element("label", h.Input(h.type("checkbox"), h.data("name", "x[]"), h.value("1")), h.Span("A"))),
                    h.Div(h.Element("label", h.Input(h.type("checkbox"), h.data("name", "x[]"), h.value("2")), h.Span("B"))),
                    h.Div(h.Element("label", h.Input(h.type("checkbox"), h.data("name", "x[]"), h.value("3")), h.Span("C"))),
                    h.Input(h.type("button"), h.value("send"), h.onclick(";"), new hdata { { "command", "array-name" }, { "container", "array-name-container" } }),
                    h.Div(state.ArrayNameResult)
                ),
                h.Div
                (

                    h.data("name", "two-question-mark-container"),
                    h.Div("two question mark bug"),
                    h.TextArea("??", h.data("name", "text")),
                    h.Input(h.type("button"), h.value("send"), h.onclick(";"), new hdata { { "command", "two-question-mark" }, { "container", "two-question-mark-container" } })
                ),
                h.Div(
                    h.style("border:1px solid lightgray"),
                    h.Div("Data attributes"),
                    h.Div(h.style("padding:5px"), h.Attribute("data-x", 12), h.Attribute("data-xs", "[12, 13]"), h.Attribute("data-x-r", "r"), "text")
                    )

              )
            );
            return page;
        }



        static readonly HBuilder h = null;

    }
    class MainState
    {
        public MainState(string text = null, string arrayNameResult = null)
        {
            this.Text = text;
            this.ArrayNameResult = arrayNameResult;
        }
        public readonly string Text;
        public readonly string ArrayNameResult;
        public MainState With(Option<string> text = null, Option<string> arrayNameResult = null)
        {
            return new MainState(text: text.Else(Text), arrayNameResult: arrayNameResult.Else(ArrayNameResult));
        }
    }

}