namespace Mistforge
{
    /// Fully aggregated combat stats for one character:
    /// base + equipment primaries + affixes + set bonuses.
    public struct StatBlock
    {
        public int maxHP;
        public int maxAP;
        public int atk;
        public int def;
        public float critPct;
        public float apGainPct;
        public float artDmgPct;
        public bool igniteOnHit;   // Emberwake 3-piece
    }
}
