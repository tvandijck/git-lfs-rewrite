using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace git_lfs_rewrite
{
    class GitTree : GitObject
    {
        public class Entry
        {
            public int Mode;
            public string Name;
            public string ObjectHash;
            public GitObject Object;
        }

        private readonly List<Entry> m_entries = new List<Entry>();

        public GitTree(string sha1, long length, Stream data)
            : base(sha1)
        {
            for (; ; )
            {
                var str = Utils.ReadString(data);
                if (string.IsNullOrEmpty(str))
                    break;

                var idx = str.IndexOf(' ');
                var entry = new Entry
                {
                    Mode = Convert.ToInt32(str.Substring(0, idx), 16),
                    Name = str.Substring(idx + 1),
                    ObjectHash = Utils.ReadSHA1(data),
                };

                m_entries.Add(entry);
            }
        }

        public GitTree(IEnumerable<Entry> entries)
            : base(null)
        {
            m_entries.AddRange(entries);
        }

        public override void Resolve(GitRepository repo)
        {
            bool hasInvalidRefs = false;
            foreach (var e in m_entries)
            {
                e.Object = repo.GetObject(e.ObjectHash);
                hasInvalidRefs |= (e.Object == null);
            }

            if (hasInvalidRefs)
            {
                Console.WriteLine("tree {0}", SHA1);
                foreach (var e in m_entries)
                {
                    if (e.Object == null)
                    {
                        Console.WriteLine("   {0}, {1}, {2}", ModeToString(e.Mode), e.Name, e.ObjectHash);
                    }
                }
            }
        }

        public void Add(Entry entry)
        {
            foreach (var e in m_entries)
            {
                if (e.Name == entry.Name)
                    return;
            }

            m_entries.Add(new Entry()
            {
                Mode = entry.Mode,
                Name = entry.Name,
                ObjectHash = string.Empty,
                Object = entry.Object
            });
        }

        class SortByName : IComparer<Entry>
        {
            public int Compare(Entry x, Entry y)
            {
                string nX = x.Name + (IsDirectory(x) ? "/" : string.Empty);
                string nY = y.Name + (IsDirectory(y) ? "/" : string.Empty);
                return string.Compare(nX, nY, StringComparison.Ordinal);
            }
        }

        public override bool Save(GitRepository repo)
        {
            m_saved = true;

            if (!IsDirty())
                return false;

            m_entries.Sort(new SortByName());
            using (var stream = new MemoryStream())
            {
                foreach (var e in m_entries)
                {
                    if (e.Object != null)
                        e.ObjectHash = e.Object.SHA1;

                    Utils.WriteString(stream, string.Format("{0:X} {1}", e.Mode, e.Name));
                    Utils.WriteSHA1(stream, e.ObjectHash);
                }

                var data = stream.ToArray();
                SHA1 = repo.WriteObject("tree", data);
            }

            return true;
        }

        private bool IsDirty()
        {
            return m_entries.Where(e => e.Object != null).Any(e => e.Object.SHA1 != e.ObjectHash);
        }

        public IEnumerable<Entry> Entries { get { return m_entries; } }


        private static bool IsDirectory(Entry x)
        {
            return ((x.Mode & 0x040000) == 0x040000);
        }

        private static string ModeToString(int mode)
        {
            if (mode == 0x160000)
                return "commit";
            if ((mode & 0x100000) == 0x100000)
                return "file  ";
            if ((mode & 0x040000) == 0x040000)
                return "tree  ";
            return "????  ";
        }
    }
}
