using System;
using System.Collections.Generic;
using System.Web;
using NitroBolt.Wui;
using System.Linq;

namespace NitroBolt.WebSampler
{
    public class HSync : HWebSynchronizeHandler
    {
        public HSync()
          : base(new Dictionary<string, Func<object, JsonData[], HContext, HtmlResult<HElement>>>
            {
              {"index", Main.HView},
              {"part1", Part1.HView},
              {"part2", Part2.HView},
              {"auth-view", AuthView.HView},
            },
            OnFirstTransformer)
        {
        }
        public static HElement OnFirstTransformer(HElement element)
        {
            if (element == null)
                return null;
            return new HElement(element.Name, element.Attributes, element.Nodes.Where(node => (node as HElement)?.Name?.LocalName != "body"), new HElement("body"));
        }
    }
}
