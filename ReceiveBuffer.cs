using System;

namespace Granite
{
    public class ReceiveBuffer
    {
        public byte[] Buffer { get; private set; }
        public int Index { get; private set; }
        public int Available => Length - Index;
        public int Length { get; private set; }


        public ReceiveBuffer(byte[] buffer)
        {
            Buffer = buffer;
        }

        public void Reset()
        {
            Length = 0;
            Index = 0;
        }

        public void Write(int count)
        {
            Length += count;
        }

        public void Consume(int count)
        {
            if (count > Available)
            {
                throw new ArgumentOutOfRangeException("Consumed > Available");
            }

            Index += count;
            
           if (Available == 0)
            {
                Index = 0;
                Length = 0;
            }
        }

        public bool HasMessage(out GraniteMessageType messageType, out int messageLength)
        {
            if (Available < GraniteMessageHeader.HeaderLength)
            {
                messageLength = 0;
                messageType = GraniteMessageType.None;
                return false;
            }

            Span<byte> span = new Span<byte>(Buffer);
            GraniteMessageHeader.ReadHeader(ref span, out messageType, out messageLength, Index);
            int total = GraniteMessageHeader.HeaderLength + messageLength;
            return Available >= total;
        }

    }
}
