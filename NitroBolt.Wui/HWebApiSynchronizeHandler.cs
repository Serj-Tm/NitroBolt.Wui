using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NitroBolt.Functional;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NitroBolt.Wui
{
    public static class HWebApiSynchronizeHandler
    {
        public static HElement[] Scripts(string frame = null, bool isDebug = false, TimeSpan? refreshPeriod = null, string syncJsName = null)
        {
            return HtmlJavaScriptDiffer.Scripts(new HElementProvider(), isDebug: isDebug, refreshPeriod: refreshPeriod, isInlineSyncScript: false, syncJsName: syncJsName, frame: frame);
        }

        public static HttpResponseMessage Process<TState>(HttpRequestMessage request, Func<TState, JsonData[], HttpRequestMessage, HtmlResult<HElement>> page) where TState : class, new()
        {
            if (request.Method == HttpMethod.Get)
            {
                var firstResult = page(new TState(), Array<JsonData>.Empty, request);
                return new HttpResponseMessage() { Content = new StringContent(firstResult.Html.ToHtmlText(), System.Text.Encoding.UTF8, "text/html") };
            }

            var json = Parse(request.Content.ReadAsStringAsync().Result);

            var route = request.GetRouteData().Route.RouteTemplate;

            var frame = route + ":" + json.JPath("frame")?.ToString();
            var cycle = ConvertHlp.ToInt(json.JPath("cycle")).OrDefault(0);
            var prev = PopUpdate(frame, cycle);

            var json_commands = (json.JPath("commands").As<JArray>()?.Select(j => new JsonData(j)).ToArray()).OrEmpty();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var result = page(prev?.Item2?.State.As<TState>() ?? new TState(), json_commands, request);
            watch.Stop();

            var js_updates = HtmlJavaScriptDiffer.JsSync(new HElementProvider(), prev?.Item2?.Page?.Element("body"), result.Html?.Element("body")).ToArray();
            var jupdate = new Dictionary<string, object>() { { "cycle", prev.Item1 }, { "prev_cycle", cycle }, { "processed_commands", json_commands.Length }, { "updates", js_updates } };

            PushUpdate(frame, prev.Item1, result.Html, result.State, watch.Elapsed);

            return new HttpResponseMessage() { Content = new StringContent(JsonConvert.SerializeObject(jupdate), System.Text.Encoding.UTF8, "application/javascript") };
        }

        static JsonData Parse(string t)
        {
            if (t.IsNullOrEmpty())
                return new JsonData(null);
            return new JsonData(JObject.Parse(t));
        }


        public static Tuple<int, HUpdate> PopUpdate(string frame, int cycle)
        {
            for (;;)
            {
                HFrame prev;
                if (Frames.TryGetValue(frame, out prev))
                {
                    var next = prev.With(prev.Cycle + 1, prev.Updates.RemoveRange(prev.Updates.Keys.Where(key => key < cycle)));
                    if (Frames.TryUpdate(frame, next, prev))
                        return Tuple.Create(next.Cycle, prev.Updates.Find(cycle));
                }
                else
                {
                    if (Frames.TryAdd(frame, new HFrame(cycle + 1)))
                        return Tuple.Create(cycle + 1, (HUpdate)null);
                }
            }
        }
        public static void PushUpdate(string frame, int cycle, HElement page, object state, TimeSpan elapsed)
        {
            for (;;)
            {
                HFrame hframe;
                if (Frames.TryGetValue(frame, out hframe))
                {
                    var nextFrame = hframe.With(updates: hframe.Updates.Add(cycle, new HUpdate(cycle, page, state, elapsed)));
                    if (Frames.TryUpdate(frame, nextFrame, hframe))
                        return;
                }
                else
                {
                    if (Frames.TryAdd(frame, new HFrame(cycle, ImmutableDictionary<int, HUpdate>.Empty.Add(cycle, new HUpdate(cycle, page, state, elapsed)))))
                        return;
                }
            }
        }
        public static readonly ConcurrentDictionary<string, HFrame> Frames = new ConcurrentDictionary<string, HFrame>();

        public class HFrame
        {
            public HFrame(int cycle = 1, ImmutableDictionary<int, HUpdate> updates = null)
            {
                this.Cycle = cycle;
                this.Updates = updates ?? ImmutableDictionary<int, HUpdate>.Empty;
            }
            public readonly int Cycle;
            public readonly ImmutableDictionary<int, HUpdate> Updates;

            public HFrame With(int? cycle = null, ImmutableDictionary<int, HUpdate> updates = null)
            {
                return new HFrame(cycle ?? this.Cycle, updates ?? Updates);
            }
        }
        public class HUpdate
        {
            public HUpdate(int cycle, HElement page, object state, TimeSpan elapsed)
            {
                this.Cycle = cycle;
                this.Page = page;
                this.State = state;
                this.Elapsed = elapsed;
            }
            public readonly int Cycle;
            public readonly HElement Page;
            public readonly object State;
            public readonly TimeSpan Elapsed;
        }

    }
}
