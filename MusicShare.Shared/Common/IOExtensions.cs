using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MusicShare.Shared.Common
{
    public static class IOExtensions
    {

        //public static int ReadCompactInteger(this BinaryReader r)
        //{
        //    int acc = r.ReadByte();

        //    int tmp;
        //    if (acc < 128)
        //        return acc;
        //    else
        //    {
        //        acc = (acc & 0x7f) << 7;
        //        tmp = r.ReadByte();
        //        if (tmp < 128)
        //            acc = acc | tmp;
        //        else
        //        {
        //            acc = (acc | tmp & 0x7f) << 7;
        //            tmp = r.ReadByte();
        //            if (tmp < 128)
        //                acc = acc | tmp;
        //            else
        //            {
        //                acc = (acc | tmp & 0x7f) << 8;
        //                tmp = r.ReadByte();
        //                acc = acc | tmp;
        //            }
        //        }
        //    }
        //    //To sign extend a value from some number of bits to a greater number of bits just copy the sign bit into all the additional bits in the new format.
        //    //convert/sign extend the 29bit two's complement number to 32 bit
        //    int mask = 1 << 28; // mask
        //    int ret = -(acc & mask) | acc;
        //    return ret;
        //}

        //public static void WriteCompactInteger(this BinaryWriter w, int value)
        //{
        //    //Sign contraction - the high order bit of the resulting value must match every bit removed from the number
        //    //Clear 3 bits 
        //    value &= 0x1fffffff;
        //    if (value < 0x80)
        //    {
        //        w.Write((byte)value);
        //    }
        //    else
        //    {
        //        if (value < 0x4000)
        //        {
        //            w.Write((byte)(value >> 7 & 0x7f | 0x80));
        //            w.Write((byte)(value & 0x7f));
        //        }
        //        else
        //        {
        //            if (value < 0x200000)
        //            {
        //                w.Write((byte)(value >> 14 & 0x7f | 0x80));
        //                w.Write((byte)(value >> 7 & 0x7f | 0x80));
        //                w.Write((byte)(value & 0x7f));
        //            }
        //            else
        //            {
        //                w.Write((byte)(value >> 22 & 0x7f | 0x80));
        //                w.Write((byte)(value >> 15 & 0x7f | 0x80));
        //                w.Write((byte)(value >> 8 & 0x7f | 0x80));
        //                w.Write((byte)(value & 0xff));
        //            }
        //        }
        //    }
        //}

        public static void WriteBytes(this BinaryWriter w, byte[] data)
        {
            w.Write(data.Length);
            w.Write(data);
        }

        public static byte[] ReadBytes(this BinaryReader r)
        {
            var len = r.ReadInt32();
            var ret = r.ReadBytes(len);
            return ret;
        }

        public static int IndexOf(this byte[] buff, byte value, int pos)
        {
            return Array.IndexOf(buff, value, pos);
        }

        public static short UnpackInt16(this byte[] buff, int pos)
        {
            return (short)(buff[pos + 0x0] | (buff[pos + 0x1] << 0x8));
        }

        public static short UnpackInt16(this byte[] buff, ref int pos)
        {
            var ret = buff.UnpackInt16(pos);
            pos += 2;
            return ret;
        }

        public static int UnpackInt32(this byte[] buff, int pos)
        {
            return (((buff[pos + 0x0] | (buff[pos + 0x1] << 0x8)) | (buff[pos + 0x2] << 0x10)) | (buff[pos + 0x3] << 0x18));
        }

        public static int UnpackInt32(this byte[] buff, ref int pos)
        {
            var ret = buff.UnpackInt32(pos);
            pos += 4;
            return ret;
        }

        public static long UnpackInt64(this byte[] buff, int pos)
        {
            uint num = (uint)(((buff[pos + 0x0] | (buff[pos + 0x1] << 0x8)) | (buff[pos + 0x2] << 0x10)) | (buff[pos + 0x3] << 0x18));
            uint num2 = (uint)(((buff[pos + 0x4] | (buff[pos + 0x5] << 0x8)) | (buff[pos + 0x6] << 0x10)) | (buff[pos + 0x7] << 0x18));
            return (((long)num2 << 0x20) | num);
        }

        public static long UnpackInt64(this byte[] buff, ref int pos)
        {
            var ret = buff.UnpackInt64(pos);
            pos += 8;
            return ret;
        }

        public static sbyte UnpackInt8(this byte[] buff, int pos)
        {
            return (sbyte)buff[pos + 0x0];
        }

        public static sbyte UnpackInt8(this byte[] buff, ref int pos)
        {
            var ret = buff.UnpackInt8(pos);
            pos += 1;
            return ret;
        }

        public static ushort UnpackUInt16(this byte[] buff, int pos)
        {
            return (ushort)(buff[pos + 0x0] | (buff[pos + 0x1] << 0x8));
        }

        public static ushort UnpackUInt16(this byte[] buff, ref int pos)
        {
            var ret = buff.UnpackUInt16(pos);
            pos += 2;
            return ret;
        }

        public static uint UnpackUInt32(this byte[] buff, int pos)
        {
            return (uint)(((buff[pos + 0x0] | (buff[pos + 0x1] << 0x8)) | (buff[pos + 0x2] << 0x10)) | (buff[pos + 0x3] << 0x18));
        }

        public static uint UnpackUInt32(this byte[] buff, ref int pos)
        {
            var ret = buff.UnpackUInt32(pos);
            pos += 4;
            return ret;
        }

        public static ulong UnpackUInt64(this byte[] buff, int pos)
        {
            uint num = (uint)(((buff[pos + 0x0] | (buff[pos + 0x1] << 0x8)) | (buff[pos + 0x2] << 0x10)) | (buff[pos + 0x3] << 0x18));
            uint num2 = (uint)(((buff[pos + 0x4] | (buff[pos + 0x5] << 0x8)) | (buff[pos + 0x6] << 0x10)) | (buff[pos + 0x7] << 0x18));
            return ((num2 << 0x20) | num);
            throw new NotImplementedException("check last expr");
        }

        public static ulong UnpackUInt64(this byte[] buff, ref int pos)
        {
            var ret = buff.UnpackUInt64(pos);
            pos += 8;
            return ret;
        }

        public static byte UnpackUInt8(this byte[] buff, int pos)
        {
            return buff[pos];
        }

        public static byte UnpackUInt8(this byte[] buff, ref int pos)
        {
            var ret = buff.UnpackUInt8(pos);
            pos += 1;
            return ret;
        }

        public static int PackUInt8(this byte[] buff, byte value, int pos)
        {
            buff[pos + 0x0] = (byte)value;
            return 1;
        }

        public static void PackUInt8(this byte[] buff, byte value, ref int pos)
        {
            pos += buff.PackUInt8(value, pos);
        }

        public static int PackUInt16(this byte[] buff, ushort value, int pos)
        {
            buff[pos + 0x0] = (byte)value;
            buff[pos + 0x1] = (byte)(value >> 0x8);
            return 2;
        }

        public static void PackUInt16(this byte[] buff, ushort value, ref int pos)
        {
            pos += buff.PackUInt16(value, pos);
        }

        public static int PackUInt32(this byte[] buff, uint value, int pos)
        {
            buff[pos + 0x0] = (byte)value;
            buff[pos + 0x1] = (byte)(value >> 0x8);
            buff[pos + 0x2] = (byte)(value >> 0x10);
            buff[pos + 0x3] = (byte)(value >> 0x18);
            return 4;
        }

        public static void PackUInt32(this byte[] buff, uint value, ref int pos)
        {
            pos += buff.PackUInt32(value, pos);
        }

        public static int PackUInt64(this byte[] buff, ulong value, int pos)
        {
            buff[pos + 0x0] = (byte)value;
            buff[pos + 0x1] = (byte)(value >> 0x8);
            buff[pos + 0x2] = (byte)(value >> 0x10);
            buff[pos + 0x3] = (byte)(value >> 0x18);
            buff[pos + 0x4] = (byte)(value >> 0x20);
            buff[pos + 0x5] = (byte)(value >> 0x28);
            buff[pos + 0x6] = (byte)(value >> 0x30);
            buff[pos + 0x7] = (byte)(value >> 0x38);
            return 8;
        }

        public static void PackUInt64(this byte[] buff, ulong value, ref int pos)
        {
            pos += buff.PackUInt64(value, pos);
        }

        public static int PackInt8(this byte[] buff, sbyte value, int pos)
        {
            buff[pos + 0x0] = (byte)value;
            return 1;
        }

        public static void PackInt8(this byte[] buff, sbyte value, ref int pos)
        {
            pos += buff.PackInt8(value, pos);
        }

        public static int PackInt16(this byte[] buff, short value, int pos)
        {
            buff[pos + 0x0] = (byte)value;
            buff[pos + 0x1] = (byte)(value >> 0x8);
            return 2;
        }

        public static void PackInt16(this byte[] buff, short value, ref int pos)
        {
            pos += buff.PackInt16(value, pos);
        }

        public static int PackInt32(this byte[] buff, int value, int pos)
        {
            buff[pos + 0x0] = (byte)value;
            buff[pos + 0x1] = (byte)(value >> 0x8);
            buff[pos + 0x2] = (byte)(value >> 0x10);
            buff[pos + 0x3] = (byte)(value >> 0x18);
            return 4;
        }

        public static void PackInt32(this byte[] buff, int value, ref int pos)
        {
            pos += buff.PackInt32(value, pos);
        }

        public static int PackInt64(this byte[] buff, long value, int pos)
        {
            buff[pos + 0x0] = (byte)value;
            buff[pos + 0x1] = (byte)(value >> 0x8);
            buff[pos + 0x2] = (byte)(value >> 0x10);
            buff[pos + 0x3] = (byte)(value >> 0x18);
            buff[pos + 0x4] = (byte)(value >> 0x20);
            buff[pos + 0x5] = (byte)(value >> 0x28);
            buff[pos + 0x6] = (byte)(value >> 0x30);
            buff[pos + 0x7] = (byte)(value >> 0x38);
            return 8;
        }

        public static void PackInt64(this byte[] buff, long value, ref int pos)
        {
            pos += buff.PackInt64(value, pos);
        }

        public static BinaryWriter WriteChars(this BinaryWriter w, string chars)
        {
            for (int i = 0; i < chars.Length; i++)
            {
                w.Write(chars[i]);
            }

            return w;
        }

        public static int PackDecimal(this byte[] buff, decimal value, int pos)
        {
            var bits = decimal.GetBits(value);
            buff[pos + 0] = (byte)bits[0];
            buff[pos + 1] = (byte)(bits[0] >> 8);
            buff[pos + 2] = (byte)(bits[0] >> 0x10);
            buff[pos + 3] = (byte)(bits[0] >> 0x18);
            buff[pos + 4] = (byte)bits[1];
            buff[pos + 5] = (byte)(bits[1] >> 8);
            buff[pos + 6] = (byte)(bits[1] >> 0x10);
            buff[pos + 7] = (byte)(bits[1] >> 0x18);
            buff[pos + 8] = (byte)bits[2];
            buff[pos + 9] = (byte)(bits[2] >> 8);
            buff[pos + 10] = (byte)(bits[2] >> 0x10);
            buff[pos + 11] = (byte)(bits[2] >> 0x18);
            buff[pos + 12] = (byte)bits[3];
            buff[pos + 13] = (byte)(bits[3] >> 8);
            buff[pos + 14] = (byte)(bits[3] >> 0x10);
            buff[pos + 15] = (byte)(bits[3] >> 0x18);
            return 16;
        }

        public static void PackDecimal(this byte[] buff, decimal value, ref int pos)
        {
            pos += buff.PackDecimal(value, pos);
        }

        public static int PackDouble(this byte[] buff, double value, int pos)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, buff, pos, bytes.Length);
            return bytes.Length;
        }

        public static void PackDouble(this byte[] buff, double value, ref int pos)
        {
            pos += buff.PackDouble(value, pos);
        }

        public static int PackSingle(this byte[] buff, float value, int pos)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, buff, pos, bytes.Length);
            return bytes.Length;
        }

        public static void PackSingle(this byte[] buff, float value, ref int pos)
        {
            pos += buff.PackSingle(value, pos);
        }
    }
}
