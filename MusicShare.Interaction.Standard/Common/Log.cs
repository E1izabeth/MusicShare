using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Interaction.Standard.Common
{
    public class Log
    {
        public static void i(string tag, string msg)
        {
        }

        public static void i(string tag, string format, params object[] args)
        {

        }

        public static void e(string tag, string msg, Exception e = null)
        {
            Error(e, $"[{tag}] {msg}");
        }

        public static void Error(Exception ex, string msg)
        {
            Debug.Print("Error\t" + msg + Environment.NewLine + ex.ToString());
        }

        public static void Message(string tag, string msg)
        {
            Debug.Print("Message\t[" + tag + "] " + msg);
        }

        public static void TraceMethod(string msg)
        {
            var trace = new StackTrace(true);
            var allFrames = trace.GetFrames();
            var frame = allFrames.SkipWhile(f => f.GetMethod().DeclaringType != typeof(Log)).Skip(1).First();
            var method = frame.GetMethod();

            var signature = method.DeclaringType.Name + "::" + method.Name;
            var location = frame.GetFileName() == null ? string.Empty : Path.GetFileName(frame.GetFileName()) + ":" + frame.GetFileLineNumber();

            Debug.Print($"[TRACE] {signature} ({location}) {msg}");
        }
    }
}
