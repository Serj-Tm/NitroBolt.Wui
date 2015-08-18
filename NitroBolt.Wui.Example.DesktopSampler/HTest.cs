using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NitroBolt.Wui
{
  class HTest
  {
    public static void Execute()
    {
      Console.WriteLine(
        new HElement("body",
          new HElement("div",
            new HAttribute("class", "r"), 
            new HAttribute("style", "color:red;background-color:lightgray;"),
            new HAttribute("title", "1<2"),
            "test 1 < 2",
            new HElement("br"),
            new HRaw(@"<div>1</div>
")
          )
        ).ToHtmlText()
      );
    }
  }
}
