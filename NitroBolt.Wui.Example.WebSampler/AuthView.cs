using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NitroBolt.Wui;
using NitroBolt.Functional;

namespace NitroBolt.WebSampler
{
  public class AuthView
  {
    //
    //  см. также Global.Application_AuthenticateRequest
    //
    public static NitroBolt.Wui.HtmlResult<HElement> HView(object _state, JsonData[] jsons, HContext context)
    {

      var logins = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase){{"Demo", "demodemo"}, {"Test", "test"}};

      foreach (var json in jsons.OrEmpty())
      {

        switch (json.JPath("data", "command")?.ToString())
        {
          case "login":
            {
              var login = json.JPath("data", "login")?.ToString();
              var password = json.JPath("data", "password")?.ToString();
              if (logins.Find(login) == password)
                context.HttpContext.SetUserAndCookie(login);
              else
              {
                //display error
              }
            }
            break;
          case "logout":
            context.HttpContext.SetUserAndCookie(null);
            break;
          default:
            break;
        }

        if (context.HttpContext.UserName() != null)
        {
          switch (json.JPath("data", "command")?.ToString())
          {
            case "login":
              {
                var login = json.JPath("data", "login")?.ToString();
                var password = json.JPath("data", "password")?.ToString();
                if (logins.Find(login) == password)
                  context.HttpContext.SetUserAndCookie(login);
                else
                {
                  //display error
                }
              }
              break;
            case "logout":
              context.HttpContext.SetUserAndCookie(null);
              break;
            default:
              break;
          }

        }
      }
      var account = context.HttpContext.UserName();


      var page = Page(logins, account);
      return new NitroBolt.Wui.HtmlResult<HElement>
      {
        Html = page,
        State = null,
      };
    }
    static HElement Page(Dictionary<string, string> logins, string username)
    {
      return h.Html
        (
          h.Body
          (
            username != null ? h.Div(h.Span(username), h.Span(" "), h.Input(h.type("button"), new hdata{{"command", "logout"}}, h.onclick(";"), h.value("Выйти"))) : null,
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
             : null
          )
        );
    }
    static readonly HBuilder h = null;
  }
}