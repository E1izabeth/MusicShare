using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MusicShare.Droid.Services.Nfc
{
    [System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class BitmaskAttribute : Attribute
    {
        public ushort Value { get; }

        public BitmaskAttribute(ushort value)
        {
            this.Value = value;
        }
    }

    internal static class ApduStatus
    {
        public static bool Is(this ushort value, ApduStatusWord code)
        {
            var field = code.GetType().GetField(code.ToString());
            if (field == null)
                return false;

            var attr = field.GetCustomAttribute<BitmaskAttribute>();
            if (attr == null)
                return false;

            var valueCode = (ApduStatusWord)(value & attr.Value);
            return valueCode == code;
        }
    }

    internal enum ApduStatusWord : ushort
    {
        /// <summary>
        /// Успешное исполнение
        /// </summary>
        [Bitmask(0xffff)]
        Ok = 0x9000,
        /// <summary>
        /// Успешное исполнение; 61XX - ОК, но есть еще ХХ байтов данных.
        /// </summary>
        [Bitmask(0xff00)]
        OkHasData = 0x6100,
        // 
        /// <summary>
        /// Исполнение завершилось с замечаниями; 62ХХ - SW2 уточняет причины замечания. Постоянная память не была изменена.
        /// </summary>
        [Bitmask(0xff00)]
        WarnImm = 0x6200,
        /// <summary>
        /// Исполнение завершилось с замечаниями; 63ХХ - SW2 уточняет причины замечания. Постоянная память была изменена.
        /// </summary>
        [Bitmask(0xff00)]
        WarnMut = 0x6300,
        /// <summary>
        /// Ошибки при исполнении команды; 6400 - Команда не была исполнена. Постоянная память не была изменена.
        /// </summary>
        [Bitmask(0xffff)]
        ErrImm = 0x6400,
        /// <summary>
        /// Ошибки при исполнении команды; 65ХХ - Команда не была исполнена. Постоянная была изменена.
        /// </summary>
        [Bitmask(0xff00)]
        ErrMut = 0x6500,
        /// <summary>
        /// Ошибки при исполнении команды; 66ХХ - Команда не была исполнена по причинам безопасности.
        /// </summary>
        [Bitmask(0xff00)]
        ErrSec = 0x6600,
        /// <summary>
        /// Ошибки, связанные с форматом команды; 6700 - Неправильная длина команды.
        /// </summary>
        [Bitmask(0xffff)]
        ErrCmdLen = 0x6700,
        /// <summary>
        /// Ошибки, связанные с форматом команды; 6881 - Карта не поддерживает указанный логический канал. 
        /// </summary>
        [Bitmask(0xffff)]
        ErrUnsupportedLogicalChannel = 0x6881,
        /// <summary>
        /// Ошибки, связанные с форматом команды; 6882 - Карта не поддерживает указанный вид Secure Messaging.
        /// </summary>
        [Bitmask(0xffff)]
        ErrUnsupportedSecureMessaging = 0x6882,
        /// <summary>
        /// Ошибки, связанные с форматом команды; 69XX - Команда не разрешена.
        /// </summary>
        [Bitmask(0xff00)]
        ErrCommandNotAllowed = 0x6900,
        /// <summary>
        /// Ошибки, связанные с форматом команды; 6AХХ - Неправильные параметры команды.
        /// </summary>
        [Bitmask(0xff00)]
        ErrWrongCommandParams1 = 0x6A00,
        /// <summary>
        /// Ошибки, связанные с форматом команды; 6B00 - Неправильные параметры команды.
        /// </summary>
        [Bitmask(0xffff)]
        ErrWrongCommandParams2 = 0x6B00,
        /// <summary>
        /// Ошибки, связанные с форматом команды; 6CXX - Неправильный Le.
        /// </summary>
        [Bitmask(0xff00)]
        ErrWrongExpectedResultLength = 0x6C00,
        /// <summary>
        /// Ошибки, связанные с форматом команды; 6D00 - Неизвестный INS.
        /// </summary>
        [Bitmask(0xffff)]
        ErrWrongInsnCode = 0x6D00,
        /// <summary>
        /// Ошибки, связанные с форматом команды; 6E00 - Неизвестный CLA.
        /// </summary>
        [Bitmask(0xffff)]
        ErrWrongCommandClass = 0x6E00,
        /// <summary>
        /// Ошибки, связанные с форматом команды; 6F00 - Ошибка без описания.
        /// </summary>
        [Bitmask(0xffff)]
        ErrUndescribed = 0x6F00
    }

}