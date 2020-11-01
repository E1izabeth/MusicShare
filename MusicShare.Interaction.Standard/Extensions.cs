using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Interaction.Standard
{
    internal class GenericComparer<T, P> : IEqualityComparer<T>
                   where P : IEquatable<P>
    {
        public readonly Func<T, P> _accessor;

        public GenericComparer(Func<T, P> accessor) { _accessor = accessor; }

        bool IEqualityComparer<T>.Equals(T x, T y) { return _accessor(x).Equals(_accessor(y)); }
        int IEqualityComparer<T>.GetHashCode(T obj) { return _accessor(obj).GetHashCode(); }
    }

    internal static class GenericComparer<T>
    {
        public static GenericComparer<T, P> By<P>(Func<T, P> accessor)
            where P : IEquatable<P>
        {
            return new GenericComparer<T, P>(accessor);
        }
    }

    internal static class GenericComparer
    {
        public static GenericComparer<T, P> For<T, P>(Func<T, P> accessor)
            where P : IEquatable<P>
        {
            return new GenericComparer<T, P>(accessor);
        }
    }

    public static class Extensions
    {
        public static IEnumerable<T> DistinctBy<T, P>(this IEnumerable<T> seq, Func<T, P> propertyAccessor)
            where P : IEquatable<P>
        {
            return seq.Distinct(GenericComparer.For(propertyAccessor));
        }

        public static string ToBase64(this string str)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }

        public static string FromBase64(this string str)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(str));
        }

        public static string ComputeSha256Hash(this string str, string salt)
        {
            using (SHA256Managed hashString = new SHA256Managed())
            {
                return Convert.ToBase64String(hashString.ComputeHash(Encoding.UTF8.GetBytes(str + "|" + salt)));
            }
        }

        public static T Lock<T>(this object lockObj, Func<T> act)
        {
            lock (lockObj)
            {
                return act();
            }
        }

        public static string NormalizeUrl(this string url)
        {
            var uri = new UriBuilder(url);
            if (string.IsNullOrWhiteSpace(uri.Scheme))
                uri.Scheme = "http://";

            //var scheme = uri.Scheme.ToLower();
            //var removePort = (uri.Port == 80 && scheme.StartsWith("http")) || (uri.Port == 443 && scheme.StartsWith("https"));
            //return removePort ? uri.Uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Port, UriFormat.UriEscaped) : uri.ToString();

            return uri.Uri.ToString();
        }

        public static void SafeDispose(this IDisposable obj)
        {
            try
            {
                if (obj != null)
                    obj.Dispose();
            }
            catch (Exception ex)
            {
                Debug.Print("Unexpected exception during " + obj.GetType().FullName + " dispose:" + Environment.NewLine + ex.ToString());
            }
        }

        public static string FormatExceptionOutputInfo(this Exception ex)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"--- Exception {ex.GetType().FullName} ({(string.IsNullOrWhiteSpace(ex.Message) ? string.Empty : (": " + ex.Message))}) at {{");
                FormatExceptionOutputInfoImpl(sb, ex);
                sb.AppendLine("--- } ");
                return sb.ToString();
            }
            catch (Exception ex2)
            {
                System.Diagnostics.Debug.Print(ex2.ToString());
                return ex.ToString();
            }
        }

        private static void FormatExceptionOutputInfoImpl(StringBuilder sb, Exception ex)
        {

            if (ex.InnerException != null)
            {
                var e = ex.InnerException;
                FormatExceptionOutputInfoImpl(sb, e);

                sb.AppendLine($"--- wrapped with {e.GetType().FullName} ({(string.IsNullOrWhiteSpace(e.Message) ? string.Empty : (": " + e.Message))}) at ");
            }

            var frames = new StackTrace(ex, true).GetFrames();
            var fnameLength = frames.Max(f => f.GetFileName()?.Length ?? 0);

            var lines = frames.Select(f => new {
                fileName = f.GetFileName(),
                lineNum = f.GetFileLineNumber(),
                columnNum = f.GetFileColumnNumber(),

                method = f.GetMethod(),
            }).Select(f => new {
                f.method,
                methodArgs = f.method.GetParameters().Length > 0 ? "..." : string.Empty,
                prefixString = $"{f.fileName}({f.lineNum},{f.columnNum}): "
            }).ToList();

            var prefixLength = lines.Max(l => l.prefixString.Length);
            lines.ForEach(f => sb.AppendLine($"{f.prefixString.PadRight(prefixLength, ' ')}{f.method.DeclaringType?.FullName}::{f.method.Name}({f.methodArgs})"));
        }


        public static bool IsEmpty(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static bool IsNotEmpty(this string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }

        public static void ForEach<T>(this IEnumerable<T> seq, Action<T> act)
        {
            foreach (var item in seq)
            {
                act(item);
            }
        }

        public static ReadOnlyCollection<T> AsReadOnly<T>(this IEnumerable<T> list)
        {
            return new ReadOnlyCollection<T>(list.ToArray());
        }

        public static bool HasCustomAttribute<T>(this MemberInfo prop)
            where T : Attribute
        {
            return prop.GetCustomAttributes(true).OfType<T>().Any();
        }

        public static int IndexOf<T>(this IEnumerable<T> seq, Func<T, bool> cond)
        {
            var index = 0;
            foreach (var item in seq)
            {
                if (cond(item))
                    return index;

                index++;
            }

            return -1;
        }

        public static T Parse<T>(this string str, T def = default(T))
            where T : struct
        {
            return Enum.TryParse<T>(str, true, out var x) ? x : def;
        }

        public static ErrorInfoType MakeErrorInfo(this Exception ex2, ErrorInfoType infoToFill = null)
        {
            if (ex2 == null)
                return null;

            infoToFill = infoToFill ?? new ErrorInfoType();
            infoToFill.Message = ex2.Message;
            infoToFill.StackTrace = ex2.StackTrace;
            infoToFill.TypeName = ex2.GetType().FullName;
            infoToFill.InnerError = MakeErrorInfo(ex2.InnerException);
            return infoToFill;
        }


        public static string FormatErrorInfo(this ErrorInfoType ex)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"--- Exception {ex.TypeName} ({(string.IsNullOrWhiteSpace(ex.Message) ? string.Empty : (": " + ex.Message))}) at {{");
                FormatErrorInfoImpl(sb, ex);
                sb.AppendLine("--- } ");
                return sb.ToString();
            }
            catch (Exception ex2)
            {
                System.Diagnostics.Debug.Print(ex2.ToString());
                return ex.ToString();
            }
        }

        private static void FormatErrorInfoImpl(StringBuilder sb, ErrorInfoType ex)
        {

            if (ex.InnerError != null)
            {
                var e = ex.InnerError;
                FormatErrorInfoImpl(sb, e);

                sb.AppendLine($"--- wrapped with {e.TypeName} ({(string.IsNullOrWhiteSpace(e.Message) ? string.Empty : (": " + e.Message))}) at ");
            }

            // D:\Home\Ged\portable-project.ru\runtime\src\Portable.Common\Net\Discovery\DiscoverClient.cs(81,36,81,38): warning CS0168: The variable 'ex' is declared but never used

            sb.AppendLine(ex.StackTrace);

            //var frames = new StackTrace(ex, true).GetFrames();
            //var fnameLength = frames.Max(f => f.GetFileName()?.Length ?? 0);

            //var lines = frames.Select(f => new
            //{
            //    fileName = f.GetFileName(),
            //    lineNum = f.GetFileLineNumber(),
            //    columnNum = f.GetFileColumnNumber(),

            //    method = f.GetMethod(),
            //}).Select(f => new
            //{
            //    f.method,
            //    methodArgs = f.method.GetParameters().Length > 0 ? "..." : string.Empty,
            //    prefixString = $"{f.fileName}({f.lineNum},{f.columnNum}): "
            //}).ToList();

            //var prefixLength = lines.Max(l => l.prefixString.Length);
            //lines.ForEach(f => sb.AppendLine($"{f.prefixString.PadRight(prefixLength, ' ')}{f.method.DeclaringType?.FullName}::{f.method.Name}({f.methodArgs})"));
        }
    }
}
