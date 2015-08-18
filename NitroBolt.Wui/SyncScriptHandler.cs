using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NitroBolt.Wui
{
  public class SyncScriptHandler: System.Web.IHttpHandler
  {
    public bool IsReusable
    {
      get { return true; }
    }

    public void ProcessRequest(System.Web.HttpContext context)
    {
      //context.Response.Write(Scripts().Select(script => script.ToHtmlText()).JoinToString(null));
      context.Response.Write(ScriptResource.sync_js);
    }

  }
}
