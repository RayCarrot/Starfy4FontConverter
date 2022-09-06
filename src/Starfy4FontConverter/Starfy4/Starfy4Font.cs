#nullable disable
using BinarySerializer;

namespace Starfy4FontConverter;

public class Starfy4Font : BinarySerializable
{
    // Height and width? Doesn't really make sense though since it's the same for all font files.
    public byte Byte_00 { get; set; }
    public byte Byte_01 { get; set; }

    public ushort FontSpritesCount { get; set; }
    public Pointer<Starfy4FontSprite>[] FontSprites { get; set; } // Can point to same sprites or 0 for null

    public override void SerializeImpl(SerializerObject s)
    {
        Byte_00 = s.Serialize<byte>(Byte_00, name: nameof(Byte_00));
        Byte_01 = s.Serialize<byte>(Byte_01, name: nameof(Byte_01));

        FontSpritesCount = s.Serialize<ushort>(FontSpritesCount, name: nameof(FontSpritesCount));
        FontSprites = s.SerializePointerArray(FontSprites, FontSpritesCount, nullValue: 0, name: nameof(FontSprites))?.ResolveObject(s);
    }
}