using HarmonyLib;
using System;
using VoiceChat.Codec;
using VoiceChat.Codec.Enums;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(OpusWrapper), nameof(OpusWrapper.Decode))]
    internal class OpusWrapperDecodePatch
    {
        private static bool Prefix(IntPtr st, byte[] data, int dataLength, float[] pcm, bool fec, int channels, ref int __result)
        {
            if (st == IntPtr.Zero)
            {
                MainClass.AddLog("OpusWrapper.Decode: OpusDecoder is already disposed!");
                __result = 0;
                return false;
            }

            int decode_fec = fec ? 1 : 0;
            int frame_size = pcm.Length / channels;
            int num = (data != null) ? OpusWrapper.opus_decode_float(st, data, dataLength, pcm, frame_size, decode_fec) : OpusWrapper.opus_decode_float(st, IntPtr.Zero, 0, pcm, frame_size, decode_fec);
            if (num == -4)
            {
                __result = 0;
                return false;
            }

            if (num < 0)
            {
                MainClass.AddLog($"OpusWrapper.Decode: OpusStatusCode is {(OpusStatusCode)num} instead of OK");
                __result = 0;
                return false;
            }

            __result = num;
            return false;
        }
    }
}
