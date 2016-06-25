using System;
using System.Collections.Generic;
using System.Web;
using System.Linq;
using NitroBolt.Functional;

namespace NitroBolt.Wui
{

    public class HWebSynchronizeHandler : IHttpHandler, System.Web.SessionState.IRequiresSessionState
    {
        public static HElement[] Scripts(string frame = null, bool isDebug = false, TimeSpan? refreshPeriod = null, string syncJsName = null)
        {
            return HtmlJavaScriptDiffer.Scripts(new HElementProvider(), isDebug: isDebug, refreshPeriod: refreshPeriod, isInlineSyncScript: false, syncJsName: syncJsName, frame:frame);
        }

        public static HElement[] Scripts_Inline(string handlerName, int cycle, bool isDebug = false, TimeSpan? refreshPeriod = null, string syncJsName = null)
        {
            return HtmlJavaScriptDiffer.Scripts(new HElementProvider(), isDebug: isDebug, refreshPeriod: refreshPeriod, isInlineSyncScript: false, syncJsName:syncJsName);
        }

        [Obsolete("Use constructor with Func<.., JsonData[], ..>")]
        public HWebSynchronizeHandler(Dictionary<string, Func<object, JsonData, HContext, HtmlResult<HElement>>> handlers)
        {
            this.Handlers = handlers.Select(pair => new
            {
                pair.Key,
                Value = new Func<object, JsonData[], HContext, HtmlResult<HElement>>((state, jsons, hcontext) =>
                {
                    if (jsons == null || jsons.Length == 0)
                        return pair.Value(state, null, hcontext);
                    var result = pair.Value(state, jsons[0], hcontext);
                    for (var i = 1; i < jsons.Length; ++i)
                    {
                        result = pair.Value(result.State, jsons[i], hcontext);
                    }
                    return result;
                }
            )
            }
            ).ToDictionary(pair => pair.Key, pair => pair.Value);
        }
        public HWebSynchronizeHandler(Dictionary<string, Func<object, JsonData[], HContext, HtmlResult<HElement>>> handlers, Func<HElement, HElement> onFirstHtmlTransformer = null)
        {
            this.Handlers = handlers;
            this.OnFirstHtmlTransformer = onFirstHtmlTransformer;
        }
        public readonly Dictionary<string, Func<object, JsonData[], HContext, HtmlResult<HElement>>> Handlers;
        public readonly Newtonsoft.Json.JsonSerializer JsonSerializer = Newtonsoft.Json.JsonSerializer.Create();
        public readonly Func<HElement, HElement> OnFirstHtmlTransformer;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetExpires(DateTime.Now.AddMinutes(0));
            context.Response.Cache.SetMaxAge(new TimeSpan(0));
            context.Response.AddHeader("Last-Modified", DateTime.Now.ToLongDateString());

            var handlerPair = Handlers.FirstOrDefault(pair => context.Request.Url.Segments.LastOrDefault()?.StartsWith(pair.Key + ".") ?? false);
            var handler = handlerPair.Value;
            if (handler == null)
            {
                context.Response.StatusCode = 404;
                return;
            }
            var handlerName = handlerPair.Key;

            if (context.Request.Url.AbsolutePath.EndsWith(".js"))
            {
                var prevCycle = int.Parse(context.Request.QueryString["cycle"]);
                var updates = Updates(context);
                var prevUpdate = updates.FirstOrDefault(_update => _update.Handler == handlerName && _update.Cycle == prevCycle);
                var prevPage = prevUpdate != null ? prevUpdate.Page : null;
                var prevState = prevUpdate?.State;

                var jsonTexts = context.Request.Form.GetValues("commands[]");
                JsonData[] json_commands = null;
                if (jsonTexts != null && jsonTexts.Length > 0)
                {
                    json_commands = jsonTexts.Select(text => new JsonData(JsonSerializer.Deserialize(new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(text))))).ToArray();
                }

                var watcher = new System.Diagnostics.Stopwatch();
                watcher.Start();
                var hContext = new HContext(handlerPair.Key, context);
                var result = ProcessCommands(handler, prevState, json_commands, hContext);

                var update = PushUpdate(context, handlerName, result.Html, result.State);

                var page = result.Html;

                var isPartial = page.Name.LocalName != "html";

                var js_updates = HtmlJavaScriptDiffer.JsSync(new HElementProvider(), prevPage == null ? null : isPartial ? prevPage : prevPage.Element("body"), isPartial ? page : page.Element("body")).ToArray();
                var jupdate = new Dictionary<string, object>() { { "cycle", update.Cycle }, { "prev_cycle", prevCycle }, { "processed_commands", json_commands.OrEmpty().Length }, { "updates", js_updates } };

                if (context.Request.Url.AbsolutePath.EndsWith(".text.js"))
                    context.Response.ContentType = "text/plain";
                else
                    context.Response.ContentType = "text/json";

                JsonSerializer.Serialize(context.Response.Output, jupdate);
                watcher.Stop();
                update.Elapsed = watcher.Elapsed;
            }
            else
            {
                var lastState = Updates(context).LastOrDefault(_update => _update.Handler == handlerName)?.State;

                var result = handler(lastState, Array<JsonData>.Empty, new HContext(handlerPair.Key, context));
                var page = result.Html;
                if (!context.Request.Url.AbsolutePath.EndsWith(".raw.html"))
                {
                    if (OnFirstHtmlTransformer != null)
                    {
                        page = OnFirstHtmlTransformer(page);
                        var update = PushUpdate(context, handlerName, page, result.State);

                        var head = page.Element("head") ?? new HElement("head");
                        var startHead = new HElement(head.Name,
                          head.Attributes,
                          head.Nodes,
                          HWebSynchronizeHandler.Scripts_Inline(handlerName, update.Cycle, refreshPeriod: result.RefreshPeriod ?? TimeSpan.FromSeconds(10))
                        );
                        page = new HElement("html", page.Attributes, startHead, page.Nodes.Where(node => (node as HElement)?.Name?.LocalName != "head"));
                    }
                    else
                    {
                        var head = page.Element("head") ?? new HElement("head");

                        var update = PushUpdate(context, handlerName, new HElement("html", head, new HElement("body")), result.State);

                        var startHead = new HElement(head.Name,
                          head.Attributes,
                          head.Nodes,
                          HWebSynchronizeHandler.Scripts_Inline(handlerName, update.Cycle, refreshPeriod: result.RefreshPeriod ?? TimeSpan.FromSeconds(10))
                        );
                        page = new HElement("html", startHead, new HElement("body"));
                    }
                }

                context.Response.Write(page.ToString());
            }
        }

        private static HtmlResult<HElement> ProcessCommands(Func<object, JsonData[], HContext, HtmlResult<HElement>> handler, object prevState, JsonData[] json_commands, HContext hContext)
        {
            try
            {
                return handler(prevState, json_commands.OrEmpty(), hContext);
            }
            catch (Exception exc)
            {
                if (!json_commands.OrEmpty().Any())
                    throw;

                System.Diagnostics.Trace.Write(exc);

                HtmlResult<HElement> result = null;
                
                //HACK игнорируем ошибку, чтобы избежать бесконечного зацикливания на ней
                foreach (var command in json_commands.OrEmpty())
                {
                    try
                    {
                        result =  handler(result?.State ?? prevState, new[] { command }, hContext);
                    }
                    catch (Exception exc2)
                    {
                        System.Diagnostics.Trace.Write(exc2);
                    }
                }
                return result ?? handler(prevState, Array<JsonData>.Empty, hContext);
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        static object Updates_Locker(HttpContext context)
        {
            var locker = context.Session["Wui.updates.locker"];
            if (locker == null)
            {
                locker = new object();
                context.Session["Wui.updates.locker"] = locker;
            }
            return locker;
        }

        public static UpdateCycle<HElement>[] Updates(HttpContext context)
        {
            lock (Updates_Locker(context))
            {
                return context.Session["Wui.updates"].As<UpdateCycle<HElement>[]>().OrEmpty();
            }
        }

        static UpdateCycle<HElement> PushUpdate(HttpContext context, string handlerName, HElement page, object state) //HACK updateCycle меняется после добавления в очередь
        {
            lock (Updates_Locker(context))
            {

                var updates = Updates(context);

                var lastUpdate = updates.LastOrDefault();

                var cycle = (lastUpdate != null ? lastUpdate.Cycle : 0) + 1;

                var update = new UpdateCycle<HElement>(handlerName, cycle, page, state);

                updates = updates.Skip(Math.Max(updates.Length - 100, 0)).Concat(new[] { update }).ToArray();

                context.Session["Wui.updates"] = updates;

                return update;
            }
        }

    }

    public class HContext
    {
        public HContext(string handlerName, HttpContext httpContext, Dictionary<string, object> contextValues = null)
        {
            this.HandlerName = handlerName;
            this.HttpContext = httpContext;
            this.ContextValues = contextValues;
        }
        public readonly string HandlerName;
        public readonly HttpContext HttpContext;
        public readonly Dictionary<string, object> ContextValues;
    }

    public class UpdateCycle<TElement>
    {
        public UpdateCycle(string handler, int cycle, TElement page, object state)
        {
            this.Handler = handler;
            this.Cycle = cycle;
            this.Page = page;
            this.State = state;
        }

        public readonly string Handler;
        public readonly int Cycle;
        public readonly TElement Page;
        public readonly object State;

        public TimeSpan Elapsed;
    }

    public class HtmlResult<TElement>
    {
        public object State;
        public TElement Html;
        /// <summary>
        /// Имеет силу только при первой загрузке страницы
        /// </summary>
        public TimeSpan? RefreshPeriod;
    }

}