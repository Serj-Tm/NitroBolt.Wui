using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using NitroBolt.Functional;

namespace NitroBolt.Wui.Tests
{
    [TestClass]
    public class ResponseProcessorTest
    {
        [TestMethod]
        public void ResponseProcessorApply()
        {
            var request = new HttpRequestMessage();

            var page = new Page();

            HWebApiSynchronizeHandler.Process<object>(request, page.View);

            Assert.AreEqual(true, page.IsResponsed);
        }

        [TestMethod]
        public void ResponseProcessorApply_Post()
        {
            var request = new HttpRequestMessage() { Method = HttpMethod.Post, Content = new StringContent("{}")};


            var page = new Page();

            var response = HWebApiSynchronizeHandler.Process<object>(request, page.View);

            Assert.AreEqual("application/javascript", response.Content.Headers.ContentType.MediaType);
            Assert.AreEqual(true, page.IsResponsed);
        }

        class Page
        {
            public HtmlResult View(object state, JsonData[] jsons, HttpRequestMessage request)
            {
                return new HtmlResult
                {
                    Html = h.Html(h.Body(1)),
                    ResponseProcessor = ResponseProcessor,
                };
            }
            public void ResponseProcessor(HttpResponseMessage response)
            {
                this.IsResponsed = true;
            }
            public bool IsResponsed = false;
        }
        static readonly HBuilder h = null;
    }
}
