using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace NitroBolt.WebSampler
{
  public class Global : System.Web.HttpApplication
  {

    protected void Application_Start(object sender, EventArgs e)
    {

    }

    protected void Session_Start(object sender, EventArgs e)
    {

    }

    protected void Application_BeginRequest(object sender, EventArgs e)
    {

    }

    protected void Application_AuthenticateRequest(object sender, EventArgs e)
    {
      var cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
      if (cookie != null)
      {
        try
        {
          var authTicket = FormsAuthentication.Decrypt(cookie.Value);
          this.Context.SetUserAndCookie(authTicket.Name, false);
        }
        catch
        {

        }
      }
    }

    protected void Application_Error(object sender, EventArgs e)
    {

    }

    protected void Session_End(object sender, EventArgs e)
    {

    }

    protected void Application_End(object sender, EventArgs e)
    {

    }
  }
}