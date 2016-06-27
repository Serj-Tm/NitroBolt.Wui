using NitroBolt.Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web;

namespace NitroBolt.WebSampler
{
    public static class AuthHlp
    {
        public static string UserName(this HttpRequestMessage request)
        {
            return request?.GetRequestContext()?.Principal?.Identity?.Name.EmptyAsNull();
        }

        public static Action<HttpResponseMessage> SetUserAndGetCookieSetter(this HttpRequestMessage request, string login)
        {
            var principal = login != null ? new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(login), Array<string>.Empty): null;
            HttpContext.Current.User = principal;
            Thread.CurrentPrincipal = principal;

            return response => response.SetAuthCookie(login);
        }

        public static void SetAuthCookie(this HttpResponseMessage response, string login)
        {
            if (login == null)
            {
                var authCookie = new CookieHeaderValue(System.Web.Security.FormsAuthentication.FormsCookieName, "");

                authCookie.Expires = DateTime.UtcNow.AddDays(-1);
                authCookie.HttpOnly = true;

                response.Headers.AddCookies(new[] { authCookie });
            }
            else
            {

                var authTicket = new System.Web.Security.FormsAuthenticationTicket
                  (
                     1, //version
                     login, // user name
                     DateTime.Now,             //creation
                     DateTime.Now.AddYears(50), //Expiration (you can set it to 1 month
                     true,  //Persistent
                     login
                  ); // additional informations
                var encryptedTicket = System.Web.Security.FormsAuthentication.Encrypt(authTicket);

                var authCookie = new CookieHeaderValue(System.Web.Security.FormsAuthentication.FormsCookieName, encryptedTicket);

                authCookie.Expires = authTicket.Expiration;
                authCookie.HttpOnly = true;

                response.Headers.AddCookies(new[] { authCookie });
            }
        }

        public static string UserName(this HttpContext context)
        {
            if (context != null && context.User != null && context.User.Identity != null)
                return context.User.Identity.Name.EmptyAsNull();
            return null;
        }
        public static void SetUserAndCookie(this HttpContext context, string login, bool isSetCookie = true)
        {
            if (login == null)
            {
                System.Web.Security.FormsAuthentication.SignOut();
                context.User = null;
            }
            else
            {
                if (isSetCookie)
                {
                    var authTicket = new System.Web.Security.FormsAuthenticationTicket
                      (
                         1, //version
                         login, // user name
                         DateTime.Now,             //creation
                         DateTime.Now.AddYears(50), //Expiration (you can set it to 1 month
                         true,  //Persistent
                         login
                      ); // additional informations
                    var encryptedTicket = System.Web.Security.FormsAuthentication.Encrypt(authTicket);

                    var authCookie = new HttpCookie(System.Web.Security.FormsAuthentication.FormsCookieName, encryptedTicket);

                    authCookie.Expires = authTicket.Expiration;
                    authCookie.HttpOnly = true;

                    context.Response.SetCookie(authCookie);
                }
                var principal = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(login), Array<string>.Empty);
                context.User = principal;
            }
        }
    }
}