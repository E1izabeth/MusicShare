using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MusicShare.Droid.Services.Nfc
{
    /*
CLA 	1 	класс команды
INS 	1 	код инструкции
P1 	1 	параметр №1
P2 	1 	параметр №2
L 	1 	длина данных, передаваемых карте.
Data 	L 	данные
     */

    internal abstract class ByteData
    {
        public ByteData()
        {
        }

        public byte[] ToByteArray()
        {
            return this.ToByteArrayImpl();
        }

        protected abstract byte[] ToByteArrayImpl();

        public override string ToString()
        {
            return string.Join(' ', this.ToByteArray().Select(n => Convert.ToString(n, 16).PadLeft(2, '0')));
        }
    }

    internal class PlainApduCommand : ByteData
    {
        public readonly byte cmdClass;
        public readonly byte insnCode;
        public readonly byte param1;
        public readonly byte param2;
        // public readonly byte dataLength;
        public readonly byte[] data;

        public PlainApduCommand(byte cmdClass, byte insnCode, byte param1, byte param2, params byte[] data)
        {
            this.cmdClass = cmdClass;
            this.insnCode = insnCode;
            this.param1 = param1;
            this.param2 = param2;
            this.data = data;
        }

        public static PlainApduCommand Parse(byte[] raw)
        {
            var len = raw[4];
            var data = new byte[len];
            Array.Copy(raw, 5, data, 0, len);

            return new PlainApduCommand(
                raw[0],
                raw[1],
                raw[2],
                raw[3],
                data
            );
        }

        protected override byte[] ToByteArrayImpl()
        {
            return new[]{
                cmdClass,
                insnCode,
                param1,
                param2,
                (byte)(data?.Length ?? 0),
            }.Concat(data).ToArray();
        }
    }

    internal class PlainApduResult : ByteData
    {
        public readonly byte[] data;
        public readonly ushort statusWord;

        public PlainApduResult(byte[] data, ushort statusWord)
        {
            this.data = data;
            this.statusWord = statusWord;
        }

        public static PlainApduResult Parse(byte[] raw)
        {
            var n = raw.Length;
            var status = (ushort)(raw[n - 2] << 8 | raw[n - 1]);
            var data = new byte[raw.Length - 2];
            Array.Copy(raw, 0, data, 0, data.Length);
            return new PlainApduResult(data, status);
        }

        protected override byte[] ToByteArrayImpl()
        {
            return data.Concat(new[] {
                (byte)((statusWord & 0xff00) >> 8),
                (byte)((statusWord & 0x00ff) >> 0),
            }).ToArray();
        }
    }

    internal class ApduCommand : PlainApduCommand
    {
        public readonly int? expectedResultLength;

        public ApduCommand(byte cmdClass, byte insnCode, byte param1, byte param2, byte[] data, int? expectedResultLength)
            : base(cmdClass, insnCode, param1, param2, data)
        {
            this.expectedResultLength = expectedResultLength;
        }

        public static new ApduCommand Parse(byte[] raw, bool? extLen = null, bool? extExpectedLen = null)
        {
            byte[] data;
            if (extLen.HasValue)
            {
                var len = ParseVarLen(raw, 4, extLen.Value);
                data = new byte[len];
                Array.Copy(raw, 5, data, 0, len);
            }
            else
            {
                data = null;
            }

            var erl = extExpectedLen.HasValue ? ParseVarLen(raw, raw.Length - (extExpectedLen.Value ? 3 : 1), extExpectedLen.Value) : default(int?);

            return new ApduCommand(
                raw[0],
                raw[1],
                raw[2],
                raw[3],
                data,
                erl
            );
        }

        protected override byte[] ToByteArrayImpl()
        {
            IEnumerable<byte> bytes = new[]{
                cmdClass,
                insnCode,
                param1,
                param2
            };

            if (data != null)
            {
                bytes = AppendVarLen(bytes, data.Length);

                bytes = bytes.Concat(data);
            }

            if (expectedResultLength.HasValue)
                bytes = AppendVarLen(bytes, expectedResultLength.Value);

            var result = bytes.ToArray();
            return result;
        }

        private static IEnumerable<byte> AppendVarLen(IEnumerable<byte> result, int length)
        {
            if (length < 256)
            {
                result = result.Concat(new[] { (byte)length });
            }
            else if (length == 256)
            {
                result = result.Concat(new[] { (byte)0 });
            }
            else if (length > 256 && length < 65536)
            {
                result = result.Concat(new[] {
                    (byte)0,
                    (byte)((length & 0xff00) >> 8),
                    (byte)((length & 0x00ff) >> 0),
                }).ToArray();
            }
            else if (length == 65536)
            {
                result = result.Concat(new[] {
                    (byte)0,
                    (byte)0,
                    (byte)0,
                }).ToArray();
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        private static int ParseVarLen(byte[] raw, int index, bool extended)
        {
            if (extended)
            {
                var b0 = raw[index + 0];
                var b1 = raw[index + 1];
                var b2 = raw[index + 2];
                return b0 == 0 && b1 == 0 && b2 == 0 ? 65536 : (b1 << 8 | b2);
            }
            else
            {
                var b0 = raw[index];
                return b0 == 0 ? 256 : b0;
            }
        }
    }


}