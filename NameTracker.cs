using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace NameMoreThings
{
    internal static class NameTracker
    {
        private const string TrackRpc = "NameMoreThings_Track";
        private const string ListRpc = "NameMoreThings_List";
        private const int PackageVersion = 2;
        private const int SectorsPerFrame = 400;
        private const float SweepInterval = 60f;
        private const float DoorResendInterval = 15f;

        public struct Remote
        {
            public long Uid;
            public string Name;
            public Vector3 Position;
            public int Prefab;
        }

        public static readonly Dictionary<ZDOID, Remote>[] Remotes = { new Dictionary<ZDOID, Remote>(), new Dictionary<ZDOID, Remote>() };
        private static bool s_serverHasMod;

        private static readonly Dictionary<ZDOID, NamedKind> s_tracked = new Dictionary<ZDOID, NamedKind>();
        private static readonly Dictionary<int, NamedKind> s_prefabKinds = new Dictionary<int, NamedKind>();
        private static readonly List<ZDO>[] s_send = { new List<ZDO>(), new List<ZDO>() };
        private static readonly List<ZDOID> s_gone = new List<ZDOID>();
        private static ZNetScene s_prefabScene;
        private static ZDOMan s_zdoMan;
        private static int s_sweepIndex;
        private static float s_nextSweep;
        private static float s_nextSend;
        private static float s_nextDoorResend;
        private static byte[] s_lastDoorList;

        [HarmonyPatch(typeof(Game), nameof(Game.Start))]
        private static class RegisterRpcs
        {
            private static void Postfix()
            {
                foreach (Dictionary<ZDOID, Remote> remotes in Remotes) remotes.Clear();
                s_serverHasMod = false;
                ZRoutedRpc.instance.Register<ZDOID>(TrackRpc, RPC_Track);
                ZRoutedRpc.instance.Register<ZPackage>(ListRpc, RPC_List);
            }
        }

        public static void NotifyServer(ZDOID id)
        {
            if (ZNet.instance == null || ZRoutedRpc.instance == null) return;
            if (ZNet.instance.IsServer()) Track(id);
            else if (s_serverHasMod) ZRoutedRpc.instance.InvokeRoutedRPC(TrackRpc, id);
        }

        public static void Update()
        {
            ZNet znet = ZNet.instance;
            if (znet == null || !znet.IsServer() || ZDOMan.instance == null || ZNetScene.instance == null || ZRoutedRpc.instance == null) return;
            if (s_zdoMan != ZDOMan.instance)
            {
                s_zdoMan = ZDOMan.instance;
                s_tracked.Clear();
                s_sweepIndex = 0;
                s_lastDoorList = null;
            }
            if (s_prefabScene != ZNetScene.instance) FindPrefabs();

            Sweep();
            if (Time.time >= s_nextSend)
            {
                s_nextSend = Time.time + ModConfig.MapUpdateInterval.Value;
                if (!znet.IsDedicated() || znet.GetPeers().Count > 0) SendLists();
            }
        }

        private static void FindPrefabs()
        {
            s_prefabScene = ZNetScene.instance;
            s_prefabKinds.Clear();
            foreach (GameObject prefab in s_prefabScene.m_prefabs)
            {
                if (!prefab) continue;
                if (prefab.GetComponent<Ship>()) s_prefabKinds[prefab.name.GetStableHashCode()] = NamedKind.Boat;
                else if (prefab.GetComponent<Door>()) s_prefabKinds[prefab.name.GetStableHashCode()] = NamedKind.Door;
            }
        }

        private static void Track(ZDOID id)
        {
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            if (zdo != null && s_prefabKinds.TryGetValue(zdo.GetPrefab(), out NamedKind kind)) s_tracked[id] = kind;
        }

        private static void Sweep()
        {
            if (s_sweepIndex < 0)
            {
                if (Time.time < s_nextSweep) return;
                s_sweepIndex = 0;
            }
            List<ZDO>[] sectors = s_zdoMan.m_objectsBySector;
            if (sectors == null) return;

            int processed = 0;
            while (s_sweepIndex < sectors.Length && processed < SectorsPerFrame)
            {
                List<ZDO> zdos = sectors[s_sweepIndex++];
                if (zdos == null || zdos.Count == 0) continue;
                processed++;
                foreach (ZDO zdo in zdos)
                {
                    if (s_prefabKinds.TryGetValue(zdo.GetPrefab(), out NamedKind kind) && zdo.GetString(NameData.NameKey).Length > 0) s_tracked[zdo.m_uid] = kind;
                }
            }
            if (s_sweepIndex >= sectors.Length)
            {
                s_sweepIndex = -1;
                s_nextSweep = Time.time + SweepInterval;
            }
        }

        private static void SendLists()
        {
            foreach (List<ZDO> list in s_send) list.Clear();
            s_gone.Clear();
            foreach (KeyValuePair<ZDOID, NamedKind> tracked in s_tracked)
            {
                ZDO zdo = s_zdoMan.GetZDO(tracked.Key);
                if (zdo == null)
                {
                    s_gone.Add(tracked.Key);
                    continue;
                }
                if (zdo.GetString(NameData.NameKey).Length > 0) s_send[(int)tracked.Value].Add(zdo);
            }
            foreach (ZDOID id in s_gone) s_tracked.Remove(id);

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, ListRpc, BuildList(NamedKind.Boat));

            ZPackage doors = BuildList(NamedKind.Door);
            byte[] doorBytes = doors.GetArray();
            if (Time.time >= s_nextDoorResend || !SameBytes(doorBytes, s_lastDoorList))
            {
                s_lastDoorList = doorBytes;
                s_nextDoorResend = Time.time + DoorResendInterval;
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, ListRpc, doors);
            }
        }

        private static ZPackage BuildList(NamedKind kind)
        {
            List<ZDO> zdos = s_send[(int)kind];
            var pkg = new ZPackage();
            pkg.Write(PackageVersion);
            pkg.Write((int)kind);
            pkg.Write(zdos.Count);
            foreach (ZDO zdo in zdos)
            {
                pkg.Write(zdo.m_uid);
                pkg.Write(zdo.GetLong(NameData.UidKey));
                pkg.Write(zdo.GetString(NameData.NameKey));
                pkg.Write(zdo.GetString(NameData.AuthorKey));
                pkg.Write(zdo.GetPosition());
                pkg.Write(zdo.GetPrefab());
            }
            return pkg;
        }

        private static bool SameBytes(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static void RPC_Track(long sender, ZDOID id)
        {
            if (ZNet.instance != null && ZNet.instance.IsServer()) Track(id);
        }

        private static void RPC_List(long sender, ZPackage pkg)
        {
            if (ZNet.instance == null || ZNet.instance.IsDedicated() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
            if (pkg.ReadInt() != PackageVersion) return;
            int kind = pkg.ReadInt();
            if (kind < 0 || kind >= Remotes.Length) return;
            s_serverHasMod = true;

            Dictionary<ZDOID, Remote> remotes = Remotes[kind];
            remotes.Clear();
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
            {
                ZDOID id = pkg.ReadZDOID();
                long uid = pkg.ReadLong();
                string name = pkg.ReadString();
                string author = pkg.ReadString();
                Vector3 position = pkg.ReadVector3();
                int prefab = pkg.ReadInt();
                remotes[id] = new Remote { Uid = uid, Name = NameData.Display(name, author), Position = position, Prefab = prefab };
            }
        }
    }
}
