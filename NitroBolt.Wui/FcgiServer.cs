using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NitroBolt.Wui
{
  public class FcgiServer
  {
    public static void Execute(bool isFcgiDebug, int port, Dictionary<string, Func<string, int, JsonData, HtmlResult<HElement>>> handlers)
    {
      var sync = new FcgiSynchronizer(handlers);

      var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
      socket.Listen(10);

      Console.WriteLine(string.Format("listen {0}..", port));
      for (var i = 0; ; i++)
      {
        var s = socket.Accept();
        //new System.Threading.Thread(_s =>
        //  {
        ProcessRequest(i == 0 || isFcgiDebug, sync, (Socket)s);
        //}).Start(s);
      }
    }

    private static void ProcessRequest(bool isFcgiDebug, FcgiSynchronizer sync, Socket s)
    {
      using (s)
      using (var stream = new NetworkStream(s))
      {
        Log("accepted..");

        for (var i = 0; i < 20; ++i)
        {
          if (stream.DataAvailable)
            break;
          System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(30));
          if (i == 19)
          {
            Log("cancel");
            return;
          }
        }

        var parameters = new Dictionary<string, string>();
        for (; ; )
        {
          var req = stream.ReadRequest();
          if (isFcgiDebug)
            Log(string.Format("{0}:{1}, {2}:{3}:{4}", req.Version, req.Type, req.RequestId, req.Content.Hex(), req.Padding.Hex()));

          if (req.Type == FcgiType.Params)
          {
            var _parameters = ParseParameters(req.Content);
            if (_parameters.Count > 0)
            {
              parameters = _parameters;
              if (isFcgiDebug)
              {
                Log("parameters:");
                foreach (var pair in parameters)
                  Log(string.Format("  {0}:{1}", pair.Key, pair.Value));
              }
            }
          }
          JsonData json = null;
          if (req.Type == FcgiType.Stdin && req.Content != null && req.Content.Length > 0)
          {
            if (isFcgiDebug)
              Log("Stdin text: " + System.Text.Encoding.UTF8.GetString(req.Content));
            var data = ParseQuery(System.Text.Encoding.UTF8.GetString(req.Content));
            var values = data.ContainsKey("values") ? data["values"] : null;
            if (isFcgiDebug)
              Log("  " + values);
            //var jsSerializer = new System.Net.Json.JsonTextParser();
            var jsSerializer = Newtonsoft.Json.JsonSerializer.Create();
            json = new JsonData(jsSerializer.Deserialize(new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(values))));

          }
          if (req.Type == FcgiType.Stdin && req.Padding.Length == 0)
          {
            var res_text = string.Format(@"Content-type: text/html

{0}
", sync.ProcessRequest(parameters, json));
            var res_data = System.Text.Encoding.UTF8.GetBytes(res_text);
            var res_pad = new byte[8 - res_data.Length % 8];
            var res = new FcgiRequest { Version = 1, Type = FcgiType.Stdout, RequestId = req.RequestId, Content = res_data, Padding = res_pad };
            stream.Write(res);
            stream.Write(new FcgiRequest { Version = 1, Type = FcgiType.Stdout, RequestId = req.RequestId });
            stream.Write(new FcgiRequest { Version = 1, Type = FcgiType.EndRequest, RequestId = req.RequestId, Content = new byte[8] });
            break;
          }
        }
      }
    }

    public static Dictionary<string, string> ParseQuery(string query)
    {
      var index = new Dictionary<string, string>();
      foreach (var pair in query.Split('&'))
      {
        var ss = pair.Split('=');
        if (ss.Length == 0)
          continue;
        index[ss[0]] = ParseQueryValue(ss.ElementAtOrDefault(1));
      }
      return index;
    }
    static string ParseQueryValue(string text)
    {
      if (text == null)
        return text;

      var encoding = System.Text.Encoding.UTF8;

      var builder = new StringBuilder();
      var bytes = new List<byte>();
      for (var i = 0; i < text.Length; ++i)
      {
        var ch = text[i];
        if (ch == '%' && i + 2 < text.Length)
        {
          bytes.Add((byte)(FromHexDigit(text[i + 1]) * 16 + FromHexDigit(text[i + 2])));
          i += 2;
        }
        else
        {
          if (ch == '+')
            ch = ' ';
          if (bytes.Count > 0)
          {
            builder.Append(encoding.GetString(bytes.ToArray()));
            bytes.Clear();
          }
          builder.Append(ch);
        }
      }
      if (bytes.Count > 0)
      {
        builder.Append(encoding.GetString(bytes.ToArray()));
        bytes.Clear();
      }
      return builder.ToString();
    }
    static int FromHexDigit(char ch)
    {
      return ch >= 'A' ? ch - 'A' + 10 : ch - '0';
    }
    public static Dictionary<string, string> ParseParameters(byte[] data)
    {
      var encoding = System.Text.Encoding.UTF8;

      var stream = new System.IO.MemoryStream(data);
      var index = new Dictionary<string, string>();
      for (; stream.Position < stream.Length; )
      {
        var nameLen = ReadLength(stream);
        var valueLen = ReadLength(stream);
        index[encoding.GetString(stream.ReadBytes(nameLen))] = encoding.GetString(stream.ReadBytes(valueLen));
      }
      return index;
    }
    static public int ReadLength(System.IO.Stream stream)
    {
      var b = stream.ReadByte();
      if ((b & 0x80) == 0)
        return b;
      return ((b & 0x7F) << 24) + (stream.ReadByte() << 16) + (stream.ReadByte() << 8) + stream.ReadByte();
    }
    public static void Log(string text)
    {
      if (string.IsNullOrEmpty(text))
        return;
      //System.IO.File.AppendAllText("/tmp/fcgi-text.debug.log", string.Format("{0}: {1}\r\n", DateTime.UtcNow, text));
      Console.WriteLine(text);
    }
  }


  class FcgiRequest
  {
    public byte Version;
    public FcgiType Type;
    public ushort RequestId;
    public byte[] Content = new byte[] { };
    public byte[] Padding = new byte[] { };
  }
  enum FcgiType
  {
    BeginRequest = 1,
    AbortRequest = 2,
    EndRequest = 3,
    Params = 4,
    Stdin = 5,
    Stdout = 6,
    Stderr = 7,
    Data = 8,
    GetValues = 9,
    GetValuesResult = 10,
    UnknownType = 11
  }

  static class SocketHlp
  {
    public static FcgiRequest ReadRequest(this System.IO.Stream stream)
    {
      var version = (byte)stream.ReadByte2();
      //Program.Log(string.Format("read version : {0}", version));
      var type = stream.ReadByte();
      var reqId = (ushort)(stream.ReadByte2() * 256 + stream.ReadByte2());
      var contentLen = stream.ReadByte2() * 256 + stream.ReadByte2();
      var paddingLen = stream.ReadByte2();
      var reserverd = stream.ReadByte2();
      //Program.Log(string.Format("{0}, {1}, {2}", contentLen, paddingLen, reserverd));
      var content = stream.ReadBytes(contentLen);
      var padding = stream.ReadBytes(paddingLen);
      return new FcgiRequest { Version = version, Type = (FcgiType)type, RequestId = reqId, Content = content, Padding = padding };
    }
    public static void Write(this System.IO.Stream stream, FcgiRequest req)
    {
      var mstream = new System.IO.MemoryStream();
      mstream.WriteByte(req.Version);
      mstream.WriteByte((byte)req.Type);
      mstream.WriteByte((byte)(req.RequestId / 256));
      mstream.WriteByte((byte)(req.RequestId % 256));
      mstream.WriteByte((byte)(req.Content.Length / 256));
      mstream.WriteByte((byte)(req.Content.Length % 256));
      mstream.WriteByte((byte)(req.Padding.Length));
      mstream.WriteByte(0);
      mstream.Write(req.Content, 0, req.Content.Length);
      mstream.Write(req.Padding, 0, req.Padding.Length);
      var message = mstream.ToArray();
      stream.Write(message, 0, message.Length);

      //Program.Log(string.Format("  {0}:{1}, {2}:{3}:{4}", req.Version, req.Type, req.RequestId, req.Content.Hex(), req.Padding.Hex()));
    }
    public static byte ReadByte2(this System.IO.Stream stream)
    {
      for (; ; )
      {
        var b = stream.ReadByte();
        if (b < 0)
        {
          System.Threading.Thread.Sleep(1);
          continue;
        }
        return (byte)b;
      }
    }
    public static byte[] ReadBytes(this System.IO.Stream stream, int len)
    {
      var buffer = new byte[len];
      if (len == 0)
        return buffer;
      for (int index = 0; ; )
      {
        var readed = stream.Read(buffer, index, len - index);
        index += readed;
        if (index < len)
        {
          System.Threading.Thread.Sleep(1);
          continue;
        }
        return buffer;
      }
    }
    public static byte[] Read(this Socket socket)
    {
      var buffer = new byte[10000];
      var len = socket.Receive(buffer);
      return buffer.Take(len).ToArray();
    }
    public static string Hex(this byte[] data)
    {
      return string.Join(" ", data.Select(b => b.ToString("x2")).ToArray());
    }
  }
}
