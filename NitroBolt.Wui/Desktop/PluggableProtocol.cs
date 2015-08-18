using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


namespace NitroBolt.Wui
{

  public static class WebBrowserHlp
  {
    public static HProtocolBuilder Protocol(this NitroBolt.Wui.HBuilder h)
    {
      return null;
    }

    public static WebBrowserProtocolFactory Register(this HProtocolBuilder builder, string protocolPrefix, Func<string, WebBrowserProtocolReaderContext, byte[]> reader, Guid? protocolId = null)
    {
      return WebBrowserProtocolFactory.Register(protocolPrefix, reader, protocolId);
    }
    public static WebBrowserProtocolFactory Register_ResourceProtocol(this HProtocolBuilder builder, System.Resources.ResourceManager resourceManager, string protocolPrefix = "nres")
    {
      return builder.Register(protocolPrefix, (url, context) => (byte[])resourceManager.GetObject(url.Substring(context.ProtocolPrefix.Length + 3).Trim('/', '\\').Replace('.', '_')));
    }
  }
  public class HProtocolBuilder{}

  //Подключение своего uri-протокола к web browser-у
  //Asynchronous Pluggable Protocols
  //http://msdn.microsoft.com/en-us/library/aa767916(v=vs.85).aspx

  [ComImport]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [Guid("00000001-0000-0000-C000-000000000046")]
  [ComVisible(true)]
  public interface IClassFactory
  {
    [PreserveSig()]
    int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);
    void LockServer(bool fLock);
  }

  /* Custom class to act as a class factory that create's an instance of the protocol */
  [Guid("0b9c4422-2b6e-4c2d-91b0-9016053ab1b1")]
  [ComVisible(true), ClassInterface(ClassInterfaceType.AutoDispatch)]
  public class WebBrowserProtocolFactory : IClassFactory
  {
    public WebBrowserProtocolFactory(string protocolPrefix, Func<string, WebBrowserProtocolReaderContext, byte[]> reader,  Guid? protocolId = null)
    {
      this.ProtocolPrefix = protocolPrefix;
      this.ProtocolId = protocolId ?? Guid.NewGuid();
      this.Reader = reader;
    }
    public readonly string ProtocolPrefix;
    public readonly Guid ProtocolId;
    public readonly Func<string, WebBrowserProtocolReaderContext, byte[]> Reader;
    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
      //Console.WriteLine("CreateInstance: {0}", riid);

      return Marshal.QueryInterface(Marshal.GetIUnknownForObject(new WebBrowserProtocol(new WebBrowserProtocolReaderContext(ProtocolId, ProtocolPrefix), Reader)), ref riid, out ppvObject);
      //if (res < 0)
      //  Marshal.ThrowExceptionForHR(res);
    }

    public void LockServer(bool fLock)
    {
      
    }

    public static WebBrowserProtocolFactory Register(string protocolPrefix, Func<string, WebBrowserProtocolReaderContext, byte[]> reader, Guid? protocolId = null)
    {
      IInternetSession session = null;
      CoInternetGetSession(0, ref session, 0);

      //Console.WriteLine(session);
      var _protocolId = protocolId ?? Guid.NewGuid();
      var factory = new WebBrowserProtocolFactory(protocolPrefix, reader, _protocolId);
      session.RegisterNameSpace(factory, ref _protocolId, protocolPrefix, 0, null, 0);
      return factory;
    }

    [DllImport("urlmon.dll")]
    static extern void CoInternetGetSession(UInt32 dwSessionMode /* = 0 */, ref IInternetSession ppIInternetSession, UInt32 dwReserved /* = 0 */);
  }

  public class WebBrowserProtocolReaderContext
  {
    public WebBrowserProtocolReaderContext(Guid id, string prefix)
    {
      this.ProtocolId = id;
      this.ProtocolPrefix = prefix;
    }
    public readonly Guid ProtocolId;
    public readonly string ProtocolPrefix;
  }

  //[Guid("37C1BAA9-166C-43CB-8E0F-9B000C7026BA")]
  [ComVisible(true)]
  public class WebBrowserProtocol:IInternetProtocol
  {
    public WebBrowserProtocol(WebBrowserProtocolReaderContext context, Func<string, WebBrowserProtocolReaderContext, byte[]> reader)
    {
      this.ReaderContext = context;
      this.Reader = reader;
    }
    readonly WebBrowserProtocolReaderContext ReaderContext;
    readonly Func<string, WebBrowserProtocolReaderContext, byte[]> Reader;


 
    public void LockRequest(uint dwOptions)
    {
      //Console.WriteLine("LockRequest");
      //throw new NotImplementedException();
    }

    public void UnlockRequest()
    {
      Console.WriteLine("UnlockRequest");
      //throw new NotImplementedException();
    }

    public void Start(string szURL, IInternetProtocolSink sink, IInternetBindInfo pOIBindInfo, uint grfPI, uint dwReserved)
    {
      //Console.WriteLine("Start: {0}", szURL);
      var data = Reader(szURL, ReaderContext);
      if (data != null)
      {
        this.data = data;
        this.seek = 0;
        this.Sink = sink;
        sink.ReportData(8, 1, 1);
      }
      else
      {
        data = null;
        System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(unchecked((int)0x800C0007));
      }
      //var prefix = "nres://";
      //if (szURL != null && szURL.StartsWith(prefix))
      //{
      //  var name = szURL.Substring(prefix.Length).Trim('/', '\\').Replace('.', '_');

      //  data = (byte[])StarResource.ResourceManager.GetObject(name);
      //  seek = 0;
      //  
      //  Sink.ReportData(8, 1, 1);
      //}
      //else
      //  data = null;
    }
    IInternetProtocolSink Sink;
    byte[] data;
    int seek = 0;

    public uint Read(IntPtr pv, uint cb, out uint pcbRead)
    {
      //Console.WriteLine("Read: {0}, {1}", pv, cb);
      if (data != null)
      {
        var buf = data.Skip(seek).Take((int)cb).ToArray();
        pcbRead = (uint)buf.Length;
        Marshal.Copy(buf, 0, pv, buf.Length);
        seek += buf.Length;

        return cb == buf.Length ? 0u : 1u;
      }
      pcbRead = 0;
      //Console.WriteLine("?");
      return (uint)Marshal.GetHRForException(new NotImplementedException());
    }

    public void Seek(long dlibMove, uint dwOrigin, out ulong plibNewPosition)
    {
      Console.WriteLine("Seek");
      throw new NotImplementedException();
    }

    public void Continue(IntPtr pProtocolData)
    {
      Console.WriteLine("Continue");
      throw new NotImplementedException();
    }

    public void Abort(int hrReason, uint dwOptions)
    {
      Console.WriteLine("Abort");
      throw new NotImplementedException();
    }

    public void Terminate(uint dwOptions)
    {
      Console.WriteLine("Terminate");
      data = null;
      seek = 0;
    }

    public void Suspend()
    {
      Console.WriteLine("Suspend");
      throw new NotImplementedException();
    }

    public void Resume()
    {
      Console.WriteLine("Resume");
      throw new NotImplementedException();
    }
  }

  [ComVisible(true), Guid("79eac9e7-baf9-11ce-8c82-00aa004ba90b"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
  public interface IInternetSession
  {
    
    void RegisterNameSpace(
      [In] 
      IClassFactory classFactory,
      [In] 
      ref Guid rclsid,
      [In, MarshalAs(UnmanagedType.LPWStr)] 
      string pwzProtocol,
      [In]
      int cPatterns,
      [In, MarshalAs(UnmanagedType.LPWStr)]
      string ppwzPatterns,
      [In]
      int dwReserved);

    [PreserveSig]
    int UnregisterNameSpace(
      [In] 
      IClassFactory classFactory,
      [In, MarshalAs(UnmanagedType.LPWStr)]
      string pszProtocol);

    int Dummy1();

    int Dummy2();

    int Dummy3();

    int Dummy4();

    int Dummy5();
  }

  [ComImport]
  [Guid("79EAC9E4-BAF9-11CE-8C82-00AA004BA90B")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface IInternetProtocol
  {
    //IInternetProtcolRoot
    void Start(
        [MarshalAs(UnmanagedType.LPWStr)] string szURL,
       IInternetProtocolSink Sink,
       IInternetBindInfo pOIBindInfo,
       UInt32 grfPI,
       UInt32 dwReserved);
    //void Continue(ref _tagPROTOCOLDATA pProtocolData);
    void Continue(IntPtr pProtocolData);
    void Abort(Int32 hrReason, UInt32 dwOptions);
    void Terminate(UInt32 dwOptions);
    void Suspend();
    void Resume();
    //IInternetProtocol
    [PreserveSig()]
    UInt32 Read(IntPtr pv, UInt32 cb, out UInt32 pcbRead);
    void Seek(long dlibMove, UInt32 dwOrigin, out ulong plibNewPosition);
    void LockRequest(UInt32 dwOptions);
    void UnlockRequest();
  }

  [ComImport]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("79EAC9E5-BAF9-11CE-8C82-00AA004BA90B")]
  public interface IInternetProtocolSink
  {
    void Switch(IntPtr pProtocolData);
    void ReportProgress(uint ulStatusCode, [MarshalAs(UnmanagedType.LPWStr)] string szStatusText);
    void ReportData(int grfBSCF, uint ulProgress, uint ulProgressMax);
    void ReportResult(int hrResult, uint dwError, [MarshalAs(UnmanagedType.LPWStr)] string szResult);
  }

  [ComImport]
  [Guid("79EAC9E1-BAF9-11CE-8C82-00AA004BA90B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface IInternetBindInfo
  {
    void GetBindInfo(out uint grfBINDF, IntPtr pbindinfo);
    void GetBindString(uint ulStringType, [MarshalAs(UnmanagedType.LPWStr)] ref string ppwzStr, uint cEl, ref uint pcElFetched);
  }

}
