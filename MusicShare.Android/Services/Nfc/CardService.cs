using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Nfc.CardEmulators;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MusicShare.Droid.Services.Nfc
{
    //[Service(Exported = true, Enabled = true, Permission = "android.permission.BIND_NFC_SERVICE")]
    //[IntentFilter(new[] { "android.nfc.cardemulation.action.HOST_APDU_SERVICE" }, Categories = new[] { "android.intent.category.DEFAULT" })]
    //[MetaData("android.nfc.cardemulation.host_apdu_service", Resource = "@xml/aid_list")]
    public class CardService : HostApduService
    {
        // ISO-DEP command HEADER for selecting an AID.
        // Format: [Class | Instruction | Parameter 1 | Parameter 2]
        private static readonly byte[] SELECT_APDU_HEADER = new byte[] { 0x00, 0xA4, 0x04, 0x00 };

        // "OK" status word sent in response to SELECT AID command (0x9000)
        private static readonly byte[] SELECT_OK_SW = new byte[] { 0x90, 0x00 };

        // "UNKNOWN" status word sent in response to invalid APDU command (0x0000)
        private static readonly byte[] UNKNOWN_CMD_SW = new byte[] { 0x00, 0x00 };

        public override byte[] ProcessCommandApdu(byte[] commandApdu, Bundle extras)
        {
            if (commandApdu.Length >= SELECT_APDU_HEADER.Length
                && Enumerable.SequenceEqual(commandApdu.Take(SELECT_APDU_HEADER.Length), SELECT_APDU_HEADER))
            {
                var hexString = string.Join("", Array.ConvertAll(commandApdu, b => b.ToString("X2")));
                this.SendMessageToActivity($"Recieved message from reader: {hexString}");

                var messageToReader = "Hello Reader!";
                var messageToReaderBytes = Encoding.UTF8.GetBytes(messageToReader);
                return messageToReaderBytes.Concat(SELECT_OK_SW).ToArray();
            }

            return UNKNOWN_CMD_SW;
        }

        public override void OnDeactivated(DeactivationReason reason) { }

        private void SendMessageToActivity(string msg)
        {
            Intent intent = new Intent("MSG_NAME");
            intent.PutExtra("MSG_DATA", msg);
            SendBroadcast(intent);
        }
    }
}