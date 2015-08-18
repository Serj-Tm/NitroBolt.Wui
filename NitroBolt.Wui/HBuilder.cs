using NitroBolt.Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NitroBolt.Wui
{
  public interface HBuilder { }

  public static class HBuilderHelper
  {
    public static HAttribute Attribute(this HBuilder h, HName name, object value)
    {
      return new HAttribute(name, value);
    }
    public static HElement Element(this HBuilder h, HName name, params object[] content)
    {
      return new HElement(name, content);
    }

    public static HRaw Raw(this HBuilder h, string html)
    {
      return new HRaw(html);
    }

    public static HElement[] Desktop_Scripts(this HBuilder h, bool isDebug = false)
    {
      return HDesktopSynchronizer.Scripts(isDebug:isDebug);
    }


    public static HElement Html(this HBuilder h, params object[] content)
    {
      return new HElement("html", content);
    }
    public static HElement Body(this HBuilder h, params object[] content)
    {
      return new HElement("body", content);
    }
    public static HElement Head(this HBuilder h, params object[] content)
    {
      return new HElement("head", content);
    }

    public static HElement Script(this HBuilder h, params object[] content)
    {
      return new HElement("script", content);
    }
    /// <summary>
    /// тег css.
    /// </summary>
    public static HElement Css(this HBuilder h, params object[] content)
    {
      return new HElement("style", content);
    }


    public static HElement Div(this HBuilder h, params object[] content)
    {
      return new HElement("div", content);
    }
    public static HElement P(this HBuilder h, params object[] content)
    {
      return new HElement("p", content);
    }
    public static HElement Span(this HBuilder h, params object[] content)
    {
      return new HElement("span", content);
    }
    public static HElement Br(this HBuilder h, params object[] content)
    {
      return new HElement("br", content);
    }
    public static HElement Table(this HBuilder h, params object[] content)
    {
      return new HElement("table", content);
    }
    public static HElement TBody(this HBuilder h, params object[] content)
    {
      return new HElement("tbody", content);
    }
    public static HElement Tr(this HBuilder h, params object[] content)
    {
      return new HElement("tr", content);
    }
    public static HElement Td(this HBuilder h, params object[] content)
    {
      return new HElement("td", content);
    }
    public static HElement Th(this HBuilder h, params object[] content)
    {
      return new HElement("th", content);
    }

    public static HElement A(this HBuilder h, params object[] content)
    {
      return new HElement("a", content);
    }
    public static HAttribute href(this HBuilder h, object value)
    {
      return new HAttribute("href", value);
    }

    public static HElement Img(this HBuilder h, params object[] content)
    {
      return new HElement("img", content);
    }

    public static HAttribute src(this HBuilder h, object value)
    {
      return new HAttribute("src", value);
    }

    public static HElement Ul(this HBuilder h, params object[] content)
    {
      return new HElement("ul", content);
    }
    public static HElement Li(this HBuilder h, params object[] content)
    {
      return new HElement("li", content);
    }

    public static HElement Pre(this HBuilder h, params object[] content)
    {
      return new HElement("pre", content);
    }
    public static HElement Input(this HBuilder h, params object[] content)
    {
      return new HElement("input", content);
    }
    public static HElement Button(this HBuilder h, params object[] content)
    {
      return h.Input(h.Attribute("type", "button"), content);
    }
    public static HElement TextArea(this HBuilder h, params object[] content)
    {
      return new HElement("textarea", content);
    }

    public static HElement Select(this HBuilder h, params object[] content)
    {
      return new HElement("select", content);
    }
    public static HElement Option(this HBuilder h, params object[] content)
    {
      return new HElement("option", content);
    }

    public static HAttribute style(this HBuilder h, params object[] values)
    {
      object value = null;
      if (values == null || values.Length == 0)
        value = null;
      else if (values.Length == 1)
        value = values[0];
      else
      {
        var builder = new StringBuilder();
        foreach (var val in values)
        {
          if (val == null)
            continue;
          var s = val.ToString();
          if (s.Length == 0)
            continue;
          builder.Append(s);
          if (s[s.Length - 1] != ';')
            builder.Append(';');
        }
        value = builder.ToString();
      }
      return new HAttribute("style", value);
    }
    public static HAttribute @class(this HBuilder h, params object[] values)
    {
      object value = null;
      if (values == null || values.Length == 0)
        value = null;
      else if (values.Length == 1)
        value = values[0];
      else
      {
        value = values.Where(val => val != null).Select(val => val.ToString()).Where(val => val.Length != 0).JoinToString(" ");
      }
      return new HAttribute("class", value);
    }

    public static HAttribute type(this HBuilder h, object value)
    {
      return new HAttribute("type", value);
    }
    public static HAttribute value(this HBuilder h, object value)
    {
      return new HAttribute("value", value);
    }

    public static HAttribute title(this HBuilder h, object value)
    {
      return new HAttribute("title", value);
    }

    public static HAttribute data(this HBuilder h, string name, object value)
    {
      return new HAttribute("data-" + name, value);
    }

    public static HAttribute colspan(this HBuilder h, object value)
    {
      return new HAttribute("colspan", value);
    }
    public static HAttribute rowspan(this HBuilder h, object value)
    {
      return new HAttribute("rowspan", value);
    }
    public static HAttribute selected(this HBuilder h)
    {
      return h.selected("selected");
    }
    public static HAttribute selected(this HBuilder h, object value)
    {
      return new HAttribute("selected", value);
    }

    public static HAttribute @checked(this HBuilder h)
    {
      return h.@checked("checked");
    }
    public static HAttribute @checked(this HBuilder h, object value)
    {
      return new HAttribute("checked", value);
    }

    public static HAttribute disabled(this HBuilder h)
    {
      return h.disabled("disabled");
    }
    public static HAttribute disabled(this HBuilder h, object value)
    {
      return new HAttribute("disabled", value);
    }

    public static HAttribute onclick(this HBuilder h, object value)
    {
      return new HAttribute("onclick", value);
    }
    public static HAttribute ondblclick(this HBuilder h, object value)
    {
      return new HAttribute("ondblclick", value);
    }
    public static HAttribute onchange(this HBuilder h, object value)
    {
      return new HAttribute("onchange", value);
    }
    public static HAttribute onblur(this HBuilder h, object value)
    {
      return new HAttribute("onblur", value);
    }

  }

  public class hdata : IEnumerable<HObject>
  {
    List<HObject> attributes = new List<HObject>();
    public void Add(string name, object value)
    {
      attributes.Add(h.data(name, value));
    }

    IEnumerator<HObject> IEnumerable<HObject>.GetEnumerator()
    {
      return attributes.GetEnumerator();
    }
    static readonly HBuilder h = null;

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return attributes.GetEnumerator();
    }
  }
}
