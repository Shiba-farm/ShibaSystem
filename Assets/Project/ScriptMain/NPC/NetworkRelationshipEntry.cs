using Unity.Netcode;

public struct NetworkRelationshipEntry : INetworkSerializable, System.IEquatable<NetworkRelationshipEntry>
{
    public int NpcId;
    public int HeartLevel;
    public int HeartExp; // progress สู่หัวใจดวงต่อไป

    // ── กันสแปมของขวัญรายวัน ────────────────────────────────────────────────
    public int LastGiftDay;      // WorldTimeManager.AbsoluteDayIndex ล่าสุดที่ให้ของขวัญ NPC คนนี้
    public int GiftsGivenToday;  // จำนวนของขวัญที่ให้ไปแล้วในวันนั้น (reset เมื่อ LastGiftDay เปลี่ยน)

    // ── การค้นพบของที่ชอบ/รัก (bitmask) ──────────────────────────────────────
    // bit ที่ i = ให้ item ตัวที่ index i ใน favoriteGifts/lovedGifts (นับเฉพาะช่องไม่ null)
    // ของ NPC ตัวนี้ไปแล้วอย่างน้อย 1 ครั้ง และตรงกับ tier นั้นจริง — ใช้เพื่อไม่ให้ UI
    // เฉลยของที่ชอบ/รักทั้งหมดทันทีที่พบ NPC ต้องให้ผู้เล่นลองผิดลองถูกเอง
    public ushort DiscoveredLikedMask;
    public ushort DiscoveredLovedMask;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref NpcId);
        serializer.SerializeValue(ref HeartLevel);
        serializer.SerializeValue(ref HeartExp);
        serializer.SerializeValue(ref LastGiftDay);
        serializer.SerializeValue(ref GiftsGivenToday);
        serializer.SerializeValue(ref DiscoveredLikedMask);
        serializer.SerializeValue(ref DiscoveredLovedMask);
    }

    public bool Equals(NetworkRelationshipEntry other) =>
        NpcId == other.NpcId && HeartLevel == other.HeartLevel && HeartExp == other.HeartExp &&
        LastGiftDay == other.LastGiftDay && GiftsGivenToday == other.GiftsGivenToday &&
        DiscoveredLikedMask == other.DiscoveredLikedMask && DiscoveredLovedMask == other.DiscoveredLovedMask;
}
