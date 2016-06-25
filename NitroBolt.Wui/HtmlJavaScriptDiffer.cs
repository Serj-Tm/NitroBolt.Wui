using NitroBolt.Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NitroBolt.Wui
{
  public class HtmlJavaScriptDiffer
  {
    public static TElement[] Scripts<TElement, TAttribute, TObject>(IElementProvider<TElement, TAttribute, TObject> elementProvider, bool isDebug = false, TimeSpan? refreshPeriod = null, bool isInlineSyncScript = true, string syncJsName = null, string frame = null)
    {
      return new[]
        {
          elementProvider.Element("meta",
            elementProvider.Attribute("http-equiv", "X-UA-Compatible"),
            elementProvider.Attribute("content", "IE=11")
           ),
          isDebug 
            ? elementProvider.Element("script", elementProvider.Attribute("src", "http://code.jquery.com/jquery-1.10.2.js"), "")
            : elementProvider.Element("script", elementProvider.Attribute("src", "http://code.jquery.com/jquery-1.10.2.min.js"), ""),
          isInlineSyncScript 
          ? elementProvider.Element("script", 
            elementProvider.Attribute("type", "text/javascript"),
            elementProvider.Raw(SyncScript)
          )
          : default(TElement),
          isInlineSyncScript ? default(TElement) : elementProvider.Element("script", elementProvider.Attribute("src", (!syncJsName.IsNullOrEmpty() && !syncJsName.StartsWith("/") ? "/" + syncJsName : syncJsName) ?? "/sync.js"), ""),
          isInlineSyncScript ? default(TElement) : elementProvider.Element("script", "$(function(){new ContainerSynchronizer(__ARGS__);});".Replace("__ARGS__", refreshPeriod != null ? "null, null, " + refreshPeriod.Value.TotalMilliseconds.ToString("f0") : "" ))
        };
    }
    public static string SyncScript = @"
var eventProps = ['type', 'bubbles', 'cancelable', 'eventPhase', 'timeStamp', 
     'button', 'clientX', 'clientY', 'screenX', 'screenY', 
     'keyIdentifier', 'keyLocation', 'keyCode', 'charCode', 'which',
     'altKey', 'ctrlKey', 'metaKey', 'shiftKey'
   ];
function server_element_event(element, event, data)
{
  element = $(element);
  var e = null;
  if (event != null)
  {
    e = {};
    var i;
    for (i = 0; i < eventProps.length; ++i)
      e[eventProps[i]] = event[eventProps[i]];
  }
  var element_data = element.data();
  var result_data = {};
  if (element_data.container != null)
  {
    var container = null;
    var parents = element.parents();
    var i;
    for (i = 0; i < parents.length; ++i)
    {
       if ($(parents[i]).data().name == element_data.container)
       {
         container = $(parents[i]);
         break;
       }
    }
    if (container != null)
    {
      result_data = $.extend(result_data, container.data());
      var childs = $.merge(container.find('input'), container.find('select'));
      childs = $.merge(childs, container.find('textarea'));
      var i;
      for (i = 0; i < childs.length; ++i)
      {
        var child = $(childs[i]);
        if (child.data().name != null)
        {
          result_data[child.data().name] = child.is(':checkbox') ? child.is(':checked'): child.val();
        }
      }
    }
  }
  server_event(JSON.stringify({value:element.is(':checkbox') ? element.is(':checked'):element.val(), checked:element.is(':checked'), data:$.extend(result_data, element_data, data), event:e}));
}
function find_element(current, path)
{
  var len = path.length;
  var i;
  for (i = 0; i < len; ++i)
  {
    if(!current)
      return null;
    var pentry = path[i];
//    $('#log').append('find_element: ' +  i + ' ' + pentry.index);
    if (pentry.kind == 'element')
    {
      current = current.children().eq(pentry.index);
//    $('#log').append('current_element: ' +  current);
    }
  }
  return current;
}
function is_event_name(name)
{
  if (!(name.substring(0, 2) === 'on'))
    return false;
  switch (name)
  {
    case 'onclick':
    case 'ondblclick':
    case 'onmousedown':
    case 'onmousemove':
    case 'onmouseover':
    case 'onmouseout':
    case 'onmouseup':
    case 'onkeydown':
    case 'onkeypress':
    case 'onkeyup':

    case 'onblur':
    case 'onchange':
    case 'onfocus':
    case 'onreset':
    case 'onselect':
    case 'onsubmit':

    case 'onabort':
    case 'onerror':
    case 'onload':
    case 'onresize':
    case 'onscroll':
    case 'onunload':
      return true;
  }
  return false;
}
function set_element(element, desc)
{
  if (!desc || !element)
    return;
 
  var len = !desc.e ? 0 : desc.e.length;
  var i;
  for (i = 0; i < len; ++i)
  {
    //window.external.Debug(element[0].tagName != null ? element[0].tagName:element);
    element.append(create_element(desc.e[i]));
  }
  //window.external.Debug('attrs');
  var len = !desc.a ? 0 : desc.a.length;
  for (i = 0; i < len; ++i)
  {
    if (is_event_name(desc.a[i].name))
    {
      var event = desc.a[i].name.substring(2);
      var value = desc.a[i].value;
      element.off(event);
      element.on(event, function(e){
          var res = eval(value);
          if (typeof(res) == 'boolean')
            return res;
          server_element_event(this, e);
        });
    }
    else if (desc.a[i].name.substring(0, 5) === 'data-')
    {
      element.data(desc.a[i].name.substring(5), desc.a[i].value);
    }
    else
    {
      element.attr(desc.a[i].name, desc.a[i].value);
    }
  }
  //window.external.Debug('text');
  if (desc.t != null)
  {
    element.text(desc.t.value);
  }
}
function create_element(desc)
{
  var element = $(desc.ns ? document.createElementNS(desc.ns, desc.name): document.createElement(desc.name));
  //window.external.Debug('create_element: ' + desc.a.length);
  var jsInit = null;
  var i;
  for (i = 0; i < (!desc.a ? 0 : desc.a.length); ++i)
  {
    //window.external.Debug('n: ' + desc.a[i].name);
    if (desc.a[i].name == 'js-init')
      jsInit = desc.a[i].value;
  }
  if (jsInit != null)
  {
    //window.external.Debug('js-init: ' + jsInit);
    var _this = element;
    eval(jsInit);
  }
  set_element(element, desc);
  return element;
}
function change_element(current, cmd, desc)
{
  if (!current)
    return;
  if (cmd == 'remove')
   current.remove();
  else if (cmd == 'clear')
  {
    current.empty();
  }
  else if (cmd == 'clear-all')
  {
    current.empty();
    var attributes = $.map(current[0].attributes, function(item) {return item.name;});

    $.each(attributes, function(i, item) {current.removeAttr(item);});
  }
  else if (cmd == 'set')
  {
   set_element(current, desc);
  }
  else if (cmd == 'after')
   current.after(create_element(desc));
  else if (cmd == 'insert')
   current.prepend(create_element(desc)); 
  else if (cmd == 'js-update')
  {
    var _this = current;
    eval(desc);   
  }
}
function sync_page(commands)
{
  var len = commands.length;
  var i;
  for (i = 0; i < len; ++i)
  {
    var command = commands[i];
    change_element(find_element($('body'), command.path), command.cmd, command.value);
  }
}

function sync_page_from_json(json)
{
  sync_page($.parseJSON(json));
  return true;
}

            ";
    //public static IEnumerable<object> JsSync(HElement oldBody, HElement body, params object[] path)
    //{
    //  throw new NotImplementedException();
    //}
    //static readonly HBuilder h = null;

    public static IEnumerable<object> JsSync<TElement, TAttribute, TObject>(IElementProvider<TElement, TAttribute, TObject> elementProvider, TElement oldBody, TElement body, params object[] path)
    {
      if (oldBody == null)
      {
        oldBody = elementProvider.Element("body");

        yield return new { path, cmd = "clear-all" };
      }

      var oldElements = elementProvider.Elements(oldBody).ToArray();
      var newElements = elementProvider.Elements(body).ToArray();

      var lastText = elementProvider.Texts(oldBody).JoinToString(null);
      var newText = elementProvider.Texts(body).JoinToString(null);

      var lastHtml = elementProvider.Raws(oldBody).JoinToString(null);
      var newHtml = elementProvider.Raws(body).JoinToString(null);

      if (oldElements.Length > 0 != newElements.Length > 0 && (lastText != "") != (newText != "") || lastHtml != "" && newHtml == "")
      {
        yield return new { path, cmd = "clear" };
        oldElements = Array<TElement>.Empty;
        lastText = "";
        lastHtml = "";
      }


      var left_i = 0;
      for (var i = 0; i < Math.Max(oldElements.Length, newElements.Length); ++i)
      {
        var oldChild = oldElements.ElementAtOrDefault(i);
        var newChild = newElements.ElementAtOrDefault(i);
        if (newChild == null)
          yield return new { path = path.Concat(new[] { new { kind = "element", index = left_i } }).ToArray(), cmd = "remove" };
        else if (oldChild == null || elementProvider.LocalName(oldChild) != elementProvider.LocalName(newChild) || elementProvider.Attribute_Get(oldChild, "data-id") != elementProvider.Attribute_Get(newChild, "data-id"))
        {
          if (oldChild != null)
            yield return new { path = path.Concat(new[] { new { kind = "element", index = left_i } }).ToArray(), cmd = "remove" };

          if (i == 0)
            yield return new { path, cmd = "insert", value = CreateJsFromXElement(elementProvider, newChild) };
          else
            yield return
              new
              {
                path = path.Concat(new[] { new { kind = "element", index = i - 1 } }).ToArray(),
                cmd = "after",
                value = CreateJsFromXElement(elementProvider, newChild)
              };

          left_i++;
        }
        else
        {
          foreach (var res in JsSync(elementProvider, oldChild, newChild, path.Concat(new[] { new { kind = "element", index = i } }).ToArray()))
            yield return res;

          left_i++;
        }
      }

      var attributes = elementProvider.Attributes(body).ToArray();


      var attrs = elementProvider.Attributes(oldBody)
        .OuterJoin(attributes, lastAtr => elementProvider.LocalName(lastAtr), newAtr => elementProvider.LocalName(newAtr), (lastAttr, newAttr) => new { lastAttr, newAttr })
        .Where(pair => elementProvider.Value(pair.lastAttr) != elementProvider.Value(pair.newAttr))
        .Select(pair =>
        {
          var attr = pair.lastAttr;
          if ((object)attr == null)
            attr = pair.newAttr;
          var attrName = elementProvider.LocalName(attr);
          return new { name = attrName, value = elementProvider.Value(pair.newAttr) };
        }
        )
        .ToArray();

      var t = newText != lastText ? new { value = newText } : null;
      var h = newHtml != lastHtml ? newHtml : null;
      if (attrs.Any() || t != null || h != null)
      {
        yield return new { path, cmd = "set", value = new { a = attrs, t, h } };
      }

      if (true)
      {
        var jsUpdate = attributes.FirstOrDefault(_attr => elementProvider.LocalName(_attr) == "js-update");
        if (jsUpdate != null)
          yield return new { path, cmd = "js-update", value = elementProvider.Value(jsUpdate) };
      }

    }

    public static object CreateJsFromXElement<TElement, TAttribute, TObject>(IElementProvider<TElement, TAttribute, TObject> elementProvider, TElement xelement)
    {
      var index = new Dictionary<string, object>();
      index["name"] = elementProvider.LocalName(xelement);
      if (elementProvider.Namespace(xelement) != null)
        index["ns"] = elementProvider.Namespace(xelement);
      var attrs = elementProvider.Attributes(xelement).Select(attr => new { name = elementProvider.LocalName(attr), value = elementProvider.Value(attr) }).ToArray();
      if (attrs.Any())
        index["a"] = attrs;
      var elements = elementProvider.Elements(xelement).Select(child => CreateJsFromXElement(elementProvider, child)).Where(child => child != null).ToArray();
      if (elements.Any())
        index["e"] = elements;
      var texts = elementProvider.Texts(xelement).ToArray();
      if (texts.Any())
        index["t"] = new { value = texts.JoinToString(null) };
      var raws = elementProvider.Raws(xelement).ToArray();
      if (raws.Any())
        index["h"] = raws.JoinToString(null);
      return index;
    }

  }
  public interface IElementProvider<TElement, TAttribute, TObject>
  {
    TElement Element(string name, params object[] content);
    TAttribute Attribute(string name, string value);
    TObject Raw(string text);
    IEnumerable<TAttribute> Attributes(TElement element);
    IEnumerable<TElement> Elements(TElement element);
    IEnumerable<string> Texts(TElement element);
    IEnumerable<string> Raws(TElement element);
    string LocalName(TAttribute attr);
    string LocalName(TElement element);
    string Namespace(TElement element);
    string Value(TAttribute attr);

    string Attribute_Get(TElement element, string name);
  }

  public class XElementProvider : IElementProvider<XElement, XAttribute, XObject>
  {

    public XElement Element(string name, params object[] content)
    {
      return new XElement(name, content);
    }
    public XAttribute Attribute(string name, string value)
    {
      return new XAttribute(name, value);
    }
    public XObject Raw(string text)
    {
      return XRaw.Create(text);
    }

    public IEnumerable<XAttribute> Attributes(XElement element)
    {
      return element.Attributes();
    }

    public IEnumerable<XElement> Elements(XElement element)
    {
      return element.Elements();
    }

    public IEnumerable<string> Texts(XElement element)
    {
      return element.Nodes().OfType<XText>().Select(text => text.Value);
    }
    public IEnumerable<string> Raws(XElement element)
    {
      return element.Nodes().OfType<XRaw>().Select(raw => raw.Value);
    }

    public string LocalName(XAttribute attr)
    {
      if (attr == null)
        return null;
      return attr.Name.LocalName;
    }

    public string LocalName(XElement element)
    {
      if (element == null)
        return null;
      return element.Name.LocalName;
    }
    public string Namespace(XElement element)
    {
      if (element == null)
        return null;
      return element.Name.NamespaceName;
    }

    public string Value(XAttribute attr)
    {
      return attr?.Value;
    }
    public string Attribute_Get(XElement element, string name)
    {
      return element?.Attribute(name)?.Value;
    }
  }
  public class HElementProvider : IElementProvider<HElement, HAttribute, HObject>
  {
    public HElement Element(string name, params object[] content)
    {
      return new HElement(name, content);
    }
    public HAttribute Attribute(string name, string value)
    {
      return new HAttribute(name, value);
    }
    public HObject Raw(string text)
    {
      return new HRaw(text);
    }

    public IEnumerable<HAttribute> Attributes(HElement element)
    {
      return element.Attributes;
    }

    public IEnumerable<HElement> Elements(HElement element)
    {
      return element.Elements();
    }

    public IEnumerable<string> Texts(HElement element)
    {
      return element.Nodes.OfType<HText>().Select(text => text.Text);
    }
    public IEnumerable<string> Raws(HElement element)
    {
      return element.Nodes.OfType<HRaw>().Select(raw => raw.Html);
    }

    public string LocalName(HAttribute attr)
    {
      if (attr == null)
        return null;
      return attr.Name.LocalName;
    }

    public string LocalName(HElement element)
    {
      if (element == null)
        return null;
      return element.Name.LocalName;
    }
    public string Namespace(HElement element)
    {
      if (element == null)
        return null;
      return element.Name.Namespace;
    }

    public string Value(HAttribute attr)
    {
      if (attr == null)
        return null;
      return attr.Value?.ToString();
    }
    public string Attribute_Get(HElement element, string name)
    {
      if (element == null)
        return null;
      var attr = element.Attributes.OrEmpty().FirstOrDefault(_attr => _attr.Name.LocalName == name);
      if (attr == null)
        return null;
      return attr.Value?.ToString();
    }
  }
}
