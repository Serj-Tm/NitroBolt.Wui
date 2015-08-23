using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NitroBolt.Functional;

namespace NitroBolt.Wui.Tests
{
    [TestClass]
    public class JsDifferTest
    {
        [TestMethod]
        public void NewText()
        {
            Assert.AreEqual("[{'path':[],'cmd':'set','value':{'a':[],'t':{'value':'txt'},'h':null}}]", Diff(h.Div(), h.Div("txt")));
        }
        [TestMethod]
        public void RemoveElement()
        {
            Assert.AreEqual("[{'path':[{'kind':'element','index':1}],'cmd':'remove'},{'path':[{'kind':'element','index':0}],'cmd':'after','value':{'name':'div'}},{'path':[{'kind':'element','index':2}],'cmd':'remove'}]", 
                Diff(h.Div(h.Div(),h.Span(), h.Div()), h.Div(h.Div(), h.Div())));
        }
        [TestMethod]
        public void RemoveSingleElement()
        {
            Assert.AreEqual("[{'path':[{'kind':'element','index':0}],'cmd':'remove'}]",
                Diff(h.Div(h.Span()), h.Div()));
        }

        private static string Diff(HElement h1, HElement h2)
        {
            var results = HtmlJavaScriptDiffer.JsSync(new HElementProvider(), h1, h2, Array<object>.Empty).ToArray();
            var s = JsonConvert.SerializeObject(results);
            return s.Replace('"', '\'');
        }

        public readonly static HBuilder h = null;
    }
}
