using System;
using System.Collections.Generic;
using System.Web;
using NitroBolt.Wui;

namespace NitroBolt.StateMachine
{
  public class HSync : HWebSynchronizeHandler
  {
    public HSync()
      : base(new Dictionary<string, Func<object, JsonData[], HContext, HtmlResult<HElement>>> 
        { 
          { "index", Main.HView },
        })
    {
    }
  }
}
