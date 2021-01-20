using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicShare.Interaction.Standard.Stream;

namespace MusicShare.Shared.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestPlaybackControlPacket()
        {
            var pckt = new PlaybackControlPacket();
            pckt.Operation = PlaybackControlOperation.Stop;

            var data = pckt.ToArray();
            var result = (PlaybackControlPacket)Packet.Parse(data, 0, data.Length);

            Assert.AreEqual(pckt.Operation, result.Operation);
        }

        [TestMethod]
        public void TestStampedStreamDataHeadPacket()
        {
            var pckt = new StampedStreamDataHeadPacket();
            pckt.StreamId = 123;
            pckt.Stamp = TimeSpan.FromTicks(3462345);
            pckt.SampleRate = 44100;
            pckt.IsMono = true;
            pckt.Flags = 9383812;
            var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde };
            pckt.DataFrame = new RawData(bytes, 0, bytes.Length);

            var data = pckt.ToArray();
            var result = (StampedStreamDataHeadPacket)Packet.Parse(data, 0, data.Length);

            Assert.AreEqual(pckt.StreamId, result.StreamId);
            Assert.AreEqual(pckt.Stamp, result.Stamp);
            Assert.AreEqual(pckt.SampleRate, result.SampleRate);
            Assert.AreEqual(pckt.IsMono, result.IsMono);
            Assert.AreEqual(pckt.Flags, result.Flags);
            Assert.AreEqual(pckt.DataFrame.Size, result.DataFrame.Size);
            Assert.IsTrue(Enumerable.SequenceEqual(
                pckt.DataFrame.Data.Skip(pckt.DataFrame.Offset).Take(pckt.DataFrame.Size),
                result.DataFrame.Data.Skip(result.DataFrame.Offset).Take(result.DataFrame.Size)
            ));
        }

        [TestMethod]
        public void TestStampedStreamDataBodyPacket()
        {
            var pckt = new StampedStreamDataBodyPacket();
            pckt.StreamId = 123;
            pckt.Stamp = TimeSpan.FromTicks(3462345);
            pckt.Flags = 9383812;
            var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde };
            pckt.DataFrame = new RawData(bytes, 0, bytes.Length);

            var data = pckt.ToArray();
            var result = (StampedStreamDataBodyPacket)Packet.Parse(data, 0, data.Length);

            Assert.AreEqual(pckt.StreamId, result.StreamId);
            Assert.AreEqual(pckt.Stamp, result.Stamp);
            Assert.AreEqual(pckt.Flags, result.Flags);
            Assert.AreEqual(pckt.DataFrame.Size, result.DataFrame.Size);
            Assert.IsTrue(Enumerable.SequenceEqual(
                pckt.DataFrame.Data.Skip(pckt.DataFrame.Offset).Take(pckt.DataFrame.Size),
                result.DataFrame.Data.Skip(result.DataFrame.Offset).Take(result.DataFrame.Size)
            ));
        }
    }
}
