using NitroBolt.Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NitroBolt.Wui
{
  public static class HConverter
  {
    public static HName ToHName(this System.Xml.Linq.XName name)
    {
      return new HName(name.NamespaceName, name.LocalName);
    }
    public static IEnumerable<HObject> ToHObject(this IEnumerable<System.Xml.Linq.XObject> nodes)
    {
      return nodes.OrEmpty().Select(node => ToHObject(node));
    }
    public static HObject ToHObject(this System.Xml.Linq.XObject node)
    {
      var attr = node as System.Xml.Linq.XAttribute;
      if (attr != null)
        return new HAttribute(ToHName(attr.Name), attr.Value);
      var text = node as System.Xml.Linq.XText;
      if (text != null)
        return new HText(text.Value);
      var element = node as System.Xml.Linq.XElement;
      if (element != null)
        return new HElement(ToHName(element.Name),
          ToHObject(element.Attributes().OfType<System.Xml.Linq.XObject>()),
          ToHObject(element.Nodes().OfType<System.Xml.Linq.XObject>())
        );
      return null;
    }
  }
}
