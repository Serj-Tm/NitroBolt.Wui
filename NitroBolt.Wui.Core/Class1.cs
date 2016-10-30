using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NitroBolt.Wui.Core
{
    public static class HWebApiSynchronizeHandler
    {
        public static IActionResult Process<TState>(HttpRequest request, Func<TState, JsonData[], HttpRequest, HtmlResult<HElement>> page) where TState : class, new()
        {
            return NitroBolt.Wui.HWebApiSynchronizeHandler.Process(request, HttpRequestAdapter.Instance, page);
        }
    }

    public class HttpRequestAdapter : IRequestAdapter<HttpRequest, IActionResult>
    {
        public static readonly HttpRequestAdapter Instance = new HttpRequestAdapter();

        public bool IsGetMethod(HttpRequest request) => request.Method == "GET";

        public string Content(HttpRequest request) => new StreamReader(request.Body).ReadToEnd();


        public string Frame(HttpRequest request) => "main";

        public IActionResult RawResponse(HtmlResult<HElement> result) => null;

        public IActionResult ToResponse(string content, string contentType, HtmlResult<HElement> result)
        {
            return new ContentResult() {Content = content, ContentType = contentType };
        }

    }
}
