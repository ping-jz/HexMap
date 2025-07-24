using System.IO;

[System.Serializable]
public struct HexValues
{
    public int elevation, waterLevel;
    public byte
    terrainTypeIndex,
    specialIndex,
    urbanLevel,
    farmLevel,
    plantLevel;

    public readonly void Save(BinaryWriter writer)
    {
        writer.Write(elevation);
        writer.Write(waterLevel);
        writer.Write(terrainTypeIndex);
        writer.Write(specialIndex);
        writer.Write(urbanLevel);
        writer.Write(farmLevel);
        writer.Write(plantLevel);
    }

    public static HexValues Load(BinaryReader reader)
    {
        HexValues values = default;
        values.elevation = reader.ReadInt32();
        values.waterLevel = reader.ReadInt32();
        values.terrainTypeIndex = reader.ReadByte();
        values.specialIndex = reader.ReadByte();
        values.urbanLevel = reader.ReadByte();
        values.farmLevel = reader.ReadByte();
        values.plantLevel = reader.ReadByte();
        return values;
    }

}