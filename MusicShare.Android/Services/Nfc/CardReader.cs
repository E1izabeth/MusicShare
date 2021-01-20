using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MusicShare.Droid.Services.Nfc
{
    public class CardReader : Java.Lang.Object, NfcAdapter.IReaderCallback
    {
        // ISO-DEP command HEADER for selecting an AID.
        // Format: [Class | Instruction | Parameter 1 | Parameter 2]
        private static readonly byte[] SELECT_APDU_HEADER = new byte[] { 0x00, 0xA4, 0x04, 0x00 };

        // AID for our loyalty card service.
        private static readonly string SAMPLE_LOYALTY_CARD_AID = "F123456789";

        // "OK" status word sent in response to SELECT AID command (0x9000)
        private static readonly byte[] SELECT_OK_SW = new byte[] { 0x90, 0x00 };

        public async void OnTagDiscovered(Tag tag)
        {
            IsoDep isoDep = IsoDep.Get(tag);

            if (isoDep != null)
            {
                try
                {
                    isoDep.Connect();

                    //var aidLength = (byte)(SAMPLE_LOYALTY_CARD_AID.Length / 2);
                    //var aidBytes = StringToByteArray(SAMPLE_LOYALTY_CARD_AID);
                    //var command = SELECT_APDU_HEADER
                    //    .Concat(new byte[] { aidLength })
                    //    .Concat(aidBytes)
                    //    .ToArray();

                    var cmd = new ApduCommand(0x00, 0xA4, 0x04, 0x00, Encoding.UTF8.GetBytes("F123456789"), null);
                    var command = cmd.ToByteArray();

                    /*
                        0 164 4 0         // SELECT_APDU_HEADER
                        5                 // длина AID в байтах
                        241 35 69 103 137 // SAMPLE_LOYALTY_CARD_AID (F1 23 45 67 89)
                     */

                    var result = isoDep.Transceive(command);

                    var apduResult = PlainApduResult.Parse(result);
                    if (apduResult.statusWord.Is(ApduStatusWord.Ok))
                    {

                    }

                    //var resultLength = result.Length;
                    //byte[] statusWord = { result[resultLength - 2], result[resultLength - 1] };
                    //var payload = new byte[resultLength - 2];
                    //Array.Copy(result, payload, resultLength - 2);
                    //var arrayEquals = SELECT_OK_SW.Length == statusWord.Length;
                    //if (Enumerable.SequenceEqual(SELECT_OK_SW, statusWord))
                    //{
                    //    var msg = Encoding.UTF8.GetString(payload);
                    //    await App.DisplayAlertAsync(msg);
                    //}
                }
                catch (Exception e)
                {
                    // await App.DisplayAlertAsync("Error communicating with card: " + e.Message);
                }
            }
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
    }
}