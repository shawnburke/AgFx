// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


namespace AgFx
{

    // Enable this class if you want to use AgFx from both Phone and Silverlight projects.

    //public class HttpUtility
    //{
    //    public static Func<string, string> UrlEncodeFunc;
    //    public static Func<string, string> UrlDecodeFunc;

    //    public static string UrlEncode(string strValue)
    //    {
    //        CheckFuncs();
    //        return UrlEncodeFunc(strValue);
    //    }
    //    public static string UrlDecode(string p)
    //    {
    //        CheckFuncs();
    //        return UrlDecodeFunc(p);
    //    }

    //    private static void CheckFuncs()
    //    {
    //        if (UrlEncodeFunc == null || UrlDecodeFunc == null)
    //        {

    //            PriorityQueue.AddUiWorkItem(() =>
    //            {
    //                MessageBox.Show("HttpUtility is in different places on the Silverlight and Windows Phone 7. \r\n\r\nOn Silverlight, the class is System.Windows.Browser.HttpUtility, on Windows Phone it's System.Net.HttpUtility.\r\nHandle this by setting AgFx.HttpUtilty.UrlEncodeFunc and UrlDecodeFunc to functions that map to the right HttpUtility method in the consuming project.");
    //            });
    //            throw new InvalidOperationException();
    //        }
            
    //    }
    //}
}
