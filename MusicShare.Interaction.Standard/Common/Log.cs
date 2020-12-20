using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        }

        public static void Error(Exception ex, string msg)
        {
            Debug.Print("Error\t" + msg + Environment.NewLine + ex.ToString());
        }

        public static void Message(string tag, string msg)
        {
            Debug.Print("Message\t[" + tag + "] " + msg);
        }
    }
}
