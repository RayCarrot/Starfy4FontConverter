#nullable disable
using BinarySerializer;

namespace Starfy4FontConverter;

public class Starfy4FontSprite : BinarySerializable
{
    // Size in bytes? Height + Width. Doesn't really match though.
    public byte Byte_00 { get; set; }
    public byte Byte_01 { get; set; }

    public byte[] ImgData { get; set; } // 2 bpp

    public override void SerializeImpl(SerializerObject s)
    {
        Byte_00 = s.Serialize<byte>(Byte_00, name: nameof(Byte_00));
        Byte_01 = s.Serialize<byte>(Byte_01, name: nameof(Byte_01));

        int length = Byte_00 * Byte_01;

        // Hack for small font. The actual sprite is 8x9, but seem to get rendered as 8x13 with additional padding.
        if (length == 16)
            length = 26;

        ImgData = s.SerializeArray<byte>(ImgData, length, name: nameof(ImgData));
    }
}