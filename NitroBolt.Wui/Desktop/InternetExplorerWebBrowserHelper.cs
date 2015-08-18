using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NitroBolt.Wui
{
  public static class InternetExplorerWebBrowserHelper
  {
    public const int FEATURE_DISABLE_NAVIGATION_SOUNDS = 21;
    public const int SET_FEATURE_ON_THREAD = 0x00000001;
    public const int SET_FEATURE_ON_PROCESS = 0x00000002;
    public const int SET_FEATURE_IN_REGISTRY = 0x00000004;
    public const int SET_FEATURE_ON_THREAD_LOCALMACHINE = 0x00000008;
    public const int SET_FEATURE_ON_THREAD_INTRANET = 0x00000010;
    public const int SET_FEATURE_ON_THREAD_TRUSTED = 0x00000020;
    public const int SET_FEATURE_ON_THREAD_INTERNET = 0x00000040;
    public const int SET_FEATURE_ON_THREAD_RESTRICTED = 0x00000080;


    [DllImport("urlmon.dll")]
    [PreserveSig]
    [return: MarshalAs(UnmanagedType.Error)]
    public static extern int CoInternetSetFeatureEnabled(int featureEntry, [MarshalAs(UnmanagedType.U4)] int dwFlags,
                                                         bool fEnable);

    //http://stackoverflow.com/questions/393166/how-to-disable-click-sound-in-webbrowser-control
    public static void DisableSounds()
    {
      CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_PROCESS, true);
    }

    public static void DocumentText_WaitWhileBusy(this System.Windows.Forms.WebBrowser browser, string html)
    {
      for (; ; )
      {
        if (!browser.IsBusy)
          break;
        System.Windows.Forms.Application.DoEvents();
        System.Threading.Thread.Sleep(1);
      }
      browser.DocumentText = html;
    }
  }
}
