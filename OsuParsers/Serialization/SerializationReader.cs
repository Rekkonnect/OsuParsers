using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OsuParsers.Serialization;

internal class SerializationReader : BinaryReader
{
    public SerializationReader(Stream s)
        : base(s, Encoding.UTF8) { }

    public override string ReadString()
    {
        if (0 == ReadByte())
            return null;
        return base.ReadString();
    }

    public byte[] ReadByteArray()
    {
        int len = ReadInt32();
        if (len > 0)
            return ReadBytes(len);
        if (len < 0)
            return null;
        return [];
    }

    public char[] ReadCharArray()
    {
        int len = ReadInt32();
        if (len > 0)
            return ReadChars(len);
        if (len < 0)
            return null;
        return [];
    }

    public DateTime ReadDateTime()
    {
        var ticks = ReadInt64();
        if (ticks < 0 || ticks > 3155378975999999999)
            ticks = 0;

        return new DateTime(ticks, DateTimeKind.Utc);
    }

    public List<T> ReadList<T>()
    {
        int count = ReadInt32();
        if (count < 0)
            return null;
        var d = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            var value = (T)ReadObject();
            d.Add(value);
        }

        return d;
    }

    public Dictionary<T, U> ReadDictionary<T, U>()
    {
        int count = ReadInt32();
        if (count < 0)
            return null;
        var d = new Dictionary<T, U>(count);
        for (int i = 0; i < count; i++)
        {
            var key = (T)ReadObject();
            var value = (U)ReadObject();
            d[key] = value;
        }

        return d;
    }

    public Dictionary<T, double> ReadNonZeroDoubleDictionary<T>()
    {
        int count = ReadInt32();
        if (count < 0)
            return null;
        var d = new Dictionary<T, double>();
        for (int i = 0; i < count; i++)
        {
            var key = (T)ReadObject();
            var doubleTypeByte = ReadByte();
            var value = ReadDouble();
            d[key] = value;
        }

        return d;
    }

    public object ReadObject()
    {
        ObjType t = (ObjType)ReadByte();
        switch (t)
        {
            case ObjType.Bool:
                return ReadBoolean();
            case ObjType.Byte:
                return ReadByte();
            case ObjType.UShort:
                return ReadUInt16();
            case ObjType.UInt:
                return ReadUInt32();
            case ObjType.ULong:
                return ReadUInt64();
            case ObjType.SByte:
                return ReadSByte();
            case ObjType.Short:
                return ReadInt16();
            case ObjType.Int:
                return ReadInt32();
            case ObjType.Long:
                return ReadInt64();
            case ObjType.Char:
                return ReadChar();
            case ObjType.String:
                return base.ReadString();
            case ObjType.Float:
                return ReadSingle();
            case ObjType.Double:
                return ReadDouble();
            case ObjType.Decimal:
                return ReadDecimal();
            case ObjType.DateTime:
                return ReadDateTime();
            case ObjType.ByteArray:
                return ReadByteArray();
            case ObjType.CharArray:
                return ReadCharArray();
            default:
                return null;
        }
    }
}
