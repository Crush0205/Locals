using System.Collections.Generic;

namespace Mistforge
{
    /// Matches queued directional inputs against a character's known Arts
    /// (§6.2). Prefix matching drives the live-highlight in the reference panel.
    public static class ArtMatcher
    {
        public static ArtDef Match(ArtDef[] arts, IReadOnlyList<ComboDirection> sequence)
        {
            foreach (var art in arts)
            {
                if (art.sequence.Length != sequence.Count) continue;
                if (StartsWith(art, sequence)) return art;
            }
            return null;
        }

        public static bool IsPrefix(ArtDef art, IReadOnlyList<ComboDirection> sequence)
        {
            if (sequence.Count > art.sequence.Length) return false;
            return StartsWith(art, sequence);
        }

        static bool StartsWith(ArtDef art, IReadOnlyList<ComboDirection> sequence)
        {
            for (int i = 0; i < sequence.Count; i++)
                if (art.sequence[i] != sequence[i]) return false;
            return true;
        }
    }
}
