using System;

namespace Granite
{

    // 8 byte header with length prefix and message type
    public class GraniteMessageHeader
    {
        public const int LengthOffset = 0;
        public const int TypeOffset = 4;
        public const int HeaderLength = 8;

        public static void ReadHeader(ref Span<byte> data, out GraniteMessageType messageType, out int messageLength, int offset = 0)
        {
            messageLength = ReadInt(ref data, offset + LengthOffset);
            messageLength -= 4;
            messageType = (GraniteMessageType)ReadInt(ref data, offset + TypeOffset);
        }


        public static void WriteHeader(ref Span<byte> data, GraniteMessageType messageType, int messageLength, int offset = 0)
        {
            int length = messageLength + 4;
            WriteInt(ref data, length, offset + LengthOffset);
            WriteInt(ref data, (int)messageType, offset + TypeOffset);
        }

        public static unsafe void WriteInt(ref Span<byte> data, int value, int offset = 0)
        {
            fixed (byte* ptr = &data[offset])
            {
                *(int*)ptr = value;
            }
        }

        public static unsafe int ReadInt(ref Span<byte> data, int offset = 0)
        {
            fixed (byte* ptr = &data[offset])
            {
                return *(int*)ptr;
            }
        }
    }
}
