using NitroBolt.Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NitroBolt.Wui
{
  public abstract class HObject
  {
    public abstract void ToHtmlText(StringBuilder builder, string prefix = "");
    public string ToHtmlText()
    {
      var builder = new StringBuilder();
      ToHtmlText(builder);
      return builder.ToString();
    }

    public static IEnumerable<HObject> HObjects<T>(IEnumerable<T> content)
    {
      return _HObjects(content).Where(item => item != null);
    }
    static IEnumerable<HObject> _HObjects<T>(IEnumerable<T> content)
    {
      foreach (var item in content.OrEmpty())
      {
        if (item is IEnumerable<HObject>)
        {
          foreach (var child in (item as IEnumerable<HObject>))
            yield return child;
        }
        else if (item is IEnumerable<object>)
        {
          foreach (var child in _HObjects(item as IEnumerable<object>))
            yield return child;
        }
        else if (item is IEnumerable<HElement>)
          foreach (var child in (item as IEnumerable<HElement>))
            yield return child;
        else if (item is HObject)
          yield return item as HObject;
        else if (item is string)
          yield return new HText(item as string);
        else
        {
          var value = item?.ToString();
          if (value != null)
            yield return new HText(value);
        }
      }

      //return content.Else_Empty()
      //  .SelectMany(item => (item is IEnumerable<HObject>) ? HObjects(item.As<IEnumerable<HObject>>()) 
      //    : (item is IEnumerable<object>) ? HObjects(item.As<IEnumerable<object>>())
      //    : (item is IEnumerable<HObject>) ? HObjects(item.As<IEnumerable<HObject>>())
      //    : (item is IEnumerable<HElement>) ? HObjects(item.As<IEnumerable<HElement>>())
      //    : (item is HObject) ? new[] { item.As<HObject>() }
      //    : (item is string) ? new[]{new HText(item.As<string>())}
      //    : ToHText(item)
      //  )
      //  .Where(item => item != null);        
    }
    static HObject[] ToHText(object value)
    {
      var text = value?.ToString();
      if (text == null)
        return Array<HObject>.Empty;
      return new HObject[] { new HText(text) };
    }
    public override string ToString()
    {
      return ToHtmlText();
    }
  }
  public class HDocument:HObject
  {
    public HDocument(object[] content)
    {
      this.Element = HObject.HObjects(content).OfType<HElement>().FirstOrDefault();
    }
    public readonly HElement Element;

    public override void ToHtmlText(StringBuilder builder, string prefix = "")
    {
      if (Element != null)
        Element.ToHtmlText(builder);
    }
  }
    public class HElement : HObject
    {
        public HElement(HName name, params object[] content)
        {
            this.Name = name;

            var attributes = new List<HAttribute>();
            var nodes = new List<HObject>();
            foreach (var node in HObject.HObjects(content))
            {
                if (node is HAttribute)
                    attributes.Add(node as HAttribute);
                else
                    nodes.Add(node);
            }
            this.Attributes = attributes.ToArray();
            this.Nodes = nodes.ToArray();
        }
        public readonly HName Name;
        public readonly HAttribute[] Attributes;
        public readonly HObject[] Nodes;

        public IEnumerable<HElement> Elements()
        {
            return Nodes.OfType<HElement>();
        }

        public IEnumerable<HElement> Elements(HName name)
        {
            return Elements().Where(element => element.Name.LocalName == name.LocalName && element.Name.Namespace == name.Namespace);
        }
        public HElement Element(HName name)
        {
            return Elements(name).FirstOrDefault();
        }

        public override void ToHtmlText(StringBuilder builder, string prefix = "")
        {
            builder.Append(prefix);
            builder.Append("<");
            //builder.Append(Name.ToString());
            builder.Append(Name.LocalName?.ToString());
            foreach (var attribute in Attributes)
            {
                builder.Append(' ');
                attribute.ToHtmlText(builder);
            }
            if (!Nodes.Any())
                builder.AppendLine("/>");
            else
            {
                builder.AppendLine(">");
                foreach (var node in Nodes)
                    node.ToHtmlText(builder, "  " + prefix);
                builder.Append(prefix);
                builder.Append("</");
                //builder.Append(Name.ToString());
                builder.Append(Name.LocalName?.ToString());
                builder.AppendLine(">");
            }

        }
    }
  public class HAttribute:HObject
  {
    public HAttribute(HName name, object value)
    {
      this.Name = name;
      this.Value = value;
    }
    public readonly HName Name;
    public readonly object Value;

    public override void ToHtmlText(StringBuilder builder, string prefix = "")
    {
      var s = Value?.ToString();
      if (s != null)
      {
        builder.Append(Name.ToString());
        builder.Append("='");
        builder.Append(System.Web.HttpUtility.HtmlEncode(s));
        builder.Append("'");
      }
    }
  }
  public class HText:HObject
  {
    public HText(string text)
    {
      this.Text = text;
    }
    public readonly string Text;

    public override void ToHtmlText(StringBuilder builder, string prefix = "")
    {
      builder.AppendLine(System.Web.HttpUtility.HtmlEncode(Text));
    }
  }
  public class HRaw:HObject
  {
    public HRaw(string html)
    {
      this.Html = html;
    }
    public readonly string Html;

    public override void ToHtmlText(StringBuilder builder, string prefix = "")
    {
      builder.Append(Html);
    }
  }

  public class HName
  {
    public HName(string _namespace, string localName)
    {
      this.Namespace = _namespace;
      this.LocalName = localName;
    }
    public readonly string Namespace;
    public readonly string LocalName;

    public override string ToString()
    {
      if (Namespace == null)
        return LocalName;
      return string.Format("{0}:{1}", Namespace, LocalName);
    }

    public static implicit operator HName(string name)
    {
      return new HName(null, name);
    }

  }
}
