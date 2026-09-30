using System.Collections.Generic;
using System.Text;
using Splatform;
using UnityEngine;

namespace NameMoreThings
{
    internal enum NamedKind
    {
        Boat,
        Door,
    }

    internal static class NameData
    {
        public static readonly int NameKey = "NameMoreThings_Name".GetStableHashCode();
        public static readonly int AuthorKey = "NameMoreThings_Author".GetStableHashCode();
        public static readonly int UidKey = "NameMoreThings_Uid".GetStableHashCode();

        public static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var sb = new StringBuilder(name.Length);
            foreach (char c in name) sb.Append(char.IsControl(c) ? ' ' : c);
            string clean = sb.ToString().RemoveRichTextTags().Trim();
            int max = ModConfig.MaxNameLength.Value;
            if (clean.Length > max)
            {
                if (char.IsHighSurrogate(clean[max - 1])) max--;
                clean = clean.Substring(0, max).TrimEnd();
            }
            return clean;
        }

        public static string Display(string raw, string author)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            PlatformUserID user = string.IsNullOrEmpty(author) || author == "host" ? default : new PlatformUserID(author);
            return CensorShittyWords.FilterUGC(raw, UGCType.Text, user, 0L).RemoveRichTextTags();
        }

        public static string LocalAuthor()
        {
            var localUser = PlatformManager.DistributionPlatform.LocalUser;
            return localUser.IsSignedIn ? localUser.PlatformUserID.ToString() : "host";
        }
    }

    public class NamedObject : MonoBehaviour
    {
        internal static readonly List<NamedObject> All = new List<NamedObject>();
        internal static readonly List<NamedObject> Boats = new List<NamedObject>();

        internal NamedKind Kind;

        private ZNetView _nview;
        private string _cachedRaw;
        private string _cachedAuthor;
        private string _displayName = "";
        private float _labelHeight = -1f;

        public bool IsValid => _nview != null && _nview.IsValid();
        public ZDOID Id => _nview.GetZDO().m_uid;
        public int PrefabHash => _nview.GetZDO().GetPrefab();
        public bool HasName => IsValid && _nview.GetZDO().GetString(NameData.NameKey).Length > 0;
        public long Uid => IsValid ? _nview.GetZDO().GetLong(NameData.UidKey) : 0L;

        public string DisplayName
        {
            get
            {
                if (!IsValid) return "";
                ZDO zdo = _nview.GetZDO();
                string raw = zdo.GetString(NameData.NameKey);
                string author = zdo.GetString(NameData.AuthorKey);
                if (raw != _cachedRaw || author != _cachedAuthor)
                {
                    _cachedRaw = raw;
                    _cachedAuthor = author;
                    _displayName = NameData.Display(raw, author);
                }
                return _displayName;
            }
        }

        public float LabelHeight
        {
            get
            {
                if (_labelHeight < 0f)
                {
                    float top = 0f;
                    bool found = false;
                    foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
                    {
                        top = Mathf.Max(top, meshRenderer.bounds.max.y - transform.position.y);
                        found = true;
                    }
                    _labelHeight = found ? Mathf.Clamp(top, 1f, 30f) : 3f;
                }
                return _labelHeight;
            }
        }

        private void Awake()
        {
            Kind = GetComponent<Ship>() ? NamedKind.Boat : NamedKind.Door;
            _nview = GetComponent<ZNetView>();
            if (!IsValid) return;
            All.Add(this);
            if (Kind == NamedKind.Boat) Boats.Add(this);
        }

        private void OnDestroy()
        {
            All.Remove(this);
            Boats.Remove(this);
            BoatLabels.Remove(this);
        }

        public void SetName(string name)
        {
            if (!IsValid) return;
            if (!_nview.IsOwner()) _nview.ClaimOwnership();
            ZDO zdo = _nview.GetZDO();
            if (zdo.GetLong(NameData.UidKey) == 0L) zdo.Set(NameData.UidKey, Utils.GenerateUID());
            zdo.Set(NameData.NameKey, NameData.Sanitize(name));
            zdo.Set(NameData.AuthorKey, NameData.LocalAuthor());
            NameTracker.NotifyServer(Id);
        }
    }
}
