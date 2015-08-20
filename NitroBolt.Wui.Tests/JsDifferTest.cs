using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace NitroBolt.Wui.Tests
{
    [TestClass]
    public class JsDifferTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var results = HtmlJavaScriptDiffer.JsSync(new HElementProvider(), h.Div(), h.Div("txt"), null).ToArray();
            var s = JsonConvert.SerializeObject(results);
            Assert.AreEqual("[{'path':null,'cmd':'set','value':{'a':[],'t':{'value':'txt'},'h':null}}]".Replace('\'', '"'), s);
        }

        public readonly static HBuilder h = null;
    }
}
