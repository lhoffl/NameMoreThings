using System.Collections.Generic;
using System.Globalization;

namespace NameMoreThings
{
    internal static class DisplayPrefs
    {
        private const string KeyPrefix = "NameMoreThings.";
        private const int ShowNameFlag = 1;
        private const int ShowOnMapFlag = 2;

        private static readonly Dictionary<long, int> s_flags = new Dictionary<long, int>();
        private static Player s_loadedFor;

        public static bool ShowName(long uid) => Get(uid, ShowNameFlag, ModConfig.LabelShowByDefault.Value);

        public static bool ShowOnMap(long uid, NamedKind kind) => Get(uid, ShowOnMapFlag, ModConfig.ShowOnMapByDefault(kind));

        public static void Set(long uid, bool showName, bool showOnMap)
        {
            Player player = Player.m_localPlayer;
            if (uid == 0L || player == null) return;
            Load(player);
            int flags = (showName ? ShowNameFlag : 0) | (showOnMap ? ShowOnMapFlag : 0);
            s_flags[uid] = flags;
            player.m_customData[KeyPrefix + uid.ToString("x")] = flags.ToString(CultureInfo.InvariantCulture);
        }

        private static bool Get(long uid, int flag, bool fallback)
        {
            Player player = Player.m_localPlayer;
            if (uid == 0L || player == null) return fallback;
            Load(player);
            return s_flags.TryGetValue(uid, out int flags) ? (flags & flag) != 0 : fallback;
        }

        private static void Load(Player player)
        {
            if (player == s_loadedFor) return;
            s_loadedFor = player;
            s_flags.Clear();
            foreach (KeyValuePair<string, string> entry in player.m_customData)
            {
                if (entry.Key.StartsWith(KeyPrefix)
                    && long.TryParse(entry.Key.Substring(KeyPrefix.Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long uid)
                    && int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int flags))
                {
                    s_flags[uid] = flags;
                }
            }
        }
    }
}
