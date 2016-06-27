using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NitroBolt.Wui;
using NitroBolt.Functional;
using System.Net.Http;
using System.Web.Http;

namespace NitroBolt.WebSampler
{
    public class AuthController:ApiController
    {
        [HttpGet, HttpPost]
        [Route("auth")]
        public HttpResponseMessage Route()
        {
            return HWebApiSynchronizeHandler.Process<object>(this.Request, HView);
        }

        //
        //  см. также Global.Application_AuthenticateRequest
        //
        public static NitroBolt.Wui.HtmlResult<HElement> HView(object _state, JsonData[] jsons, HttpRequestMessage request)
        {
            Action<HttpResponseMessage> cookieSetter = null;
            var logins = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) { { "Demo", "demodemo" }, { "Test", "test" } };

            foreach (var json in jsons.OrEmpty())
            {

                switch (json.JPath("data", "command")?.ToString())
                {
                    case "login":
                        {
                            var login = json.JPath("data", "login")?.ToString();
                            var password = json.JPath("data", "password")?.ToString();
                            if (logins.Find(login) == password)
                                cookieSetter = request.SetUserAndGetCookieSetter(login);
                            else
                            {
                                //display error
                            }
                        }
                        break;
                    case "logout":
                        cookieSetter = request.SetUserAndGetCookieSetter(null);
                        break;
                    default:
                        break;
                }

                if (request.UserName() != null)
                {
                    switch (json.JPath("data", "command")?.ToString())
                    {
                        default:
                            break;
                    }

                }
            }
            var account = request.UserName();


            var page = Page(logins, account, request);
            return new HtmlApiResult<HElement>
            {
                Html = page,
                State = null,
                ResponseProcessor = cookieSetter,
            };
        }
        static HElement Page(Dictionary<string, string> logins, string username, HttpRequestMessage request)
        {
            return h.Html
              (
                h.Body
                (
                  username != null ? h.Div(h.Span(username), h.Span(" "), h.Input(h.type("button"), new hdata { { "command", "logout" } }, h.onclick(";"), h.value("Выйти"))) : null,
                  username == null
                   ? h.Div
                     (
                       h.data("name", "login-box"),
                       h.Span(logins.Select(pair => string.Format("{0}({1})", pair.Key, pair.Value)).JoinToString(" или ")),
                       h.Div(h.Span("Логин: "), h.Input(h.type("text"), h.data("name", "login"))),
                       h.Div(h.Span("Пароль: "), h.Input(h.type("password"), h.data("name", "password"))),
                       h.Input(h.type("button"), new hdata { { "command", "login" }, { "container", "login-box" } }, h.onclick(";"), h.value("Войти"))
                     )
                     : null,
                  username != null
                   ? h.Div("Данные")
                   : null,
                  h.Div(request.RequestUri)
                )
              );
        }
        static readonly HBuilder h = null;
    }
}