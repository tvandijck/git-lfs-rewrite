using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zlib;

namespace git_lfs_rewrite
{
    class GitRepository
    {
        private static readonly SHA1 s_sha1 = new SHA1Managed();

        private readonly string m_path;
        private readonly string m_objectPath;
        private readonly string m_lfsPath;
        private readonly Dictionary<string, GitObject> m_gitObjects = new Dictionary<string, GitObject>();
        private readonly List<GitBranch> m_refs = new List<GitBranch>();

        public GitRepository(string path)
        {
            // load all objects.
            m_path = path;
            m_objectPath = Path.Combine(path, "objects");
            m_lfsPath = Path.Combine(path, "lfs\\objects");

            // read all objects.
            var files = Directory.GetFiles(m_objectPath, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; ++i)
            {
                Console.Write("{0}/{1}\r", i, files.Length);
                var fname = Path.GetFileName(files[i]);
                var prefix = Path.GetFileName(Path.GetDirectoryName(files[i]));
                var sha1 = prefix + fname;
                m_gitObjects.Add(sha1, ReadGitObject(sha1));
            }

            // resolve links.
            Console.WriteLine();
            Console.WriteLine("Resolving....");
            foreach (var obj in m_gitObjects.Values)
            {
                obj.Resolve(this);
            }

            // load references.
            var refs = File.ReadAllLines(Path.Combine(path, "packed-refs"));
            foreach (var r in refs)
            {
                if (r.StartsWith("#"))
                    continue;
                if (r.StartsWith("^"))
                    continue;

                var parts = r.Split(' ');
                var b = new GitBranch(parts[1], parts[0]);
                b.Resolve(this);
                m_refs.Add(b);
            }

            Console.WriteLine("Loaded {0} objects", m_gitObjects.Count);
        }

        public GitObject GetObject(string sha1)
        {
            if (sha1.Length != 40)
                return null;

            GitObject obj;
            if (!m_gitObjects.TryGetValue(sha1, out obj))
                return null;
            return obj;
        }

        public IEnumerable<GitObject> All()
        {
            return m_gitObjects.Values;
        }

        public void Save()
        {
            Console.WriteLine("Sorting...");
            // mark everything as not saved.
            foreach (var obj in m_gitObjects.Values)
                obj.Saved = false;

            // collect all commits.
            var set = CollectCommits();

            // now sort into a history.
            var commits = new List<GitCommit>();
            var roots = set.Where(c => c.Parents == null).ToArray();
            while (set.Count > 0)
            {
                commits.AddRange(roots);
                foreach (var r in roots)
                {
                    set.Remove(r);
                }

                roots = set.Where(c => !c.Parents.Any(p => set.Contains(p))).ToArray();
            }

            Console.WriteLine("Saving...");
            // save all trees first.
            foreach (var commit in commits)
            {
                SaveTree(commit.Tree);
                commit.Save(this);
            }

            // save ref-pack here.
            using (var refs = File.CreateText(Path.Combine(m_path, "packed-refs")))
            {
                refs.WriteLine("# pack-refs with: peeled fully-peeled");
                foreach (var branch in m_refs)
                {
                    if (branch.Tag != null)
                        branch.Tag.Save(this);
                    branch.Save(this);

                    refs.Write("{0} {1}\n", branch.Hash, branch.Name);
                }
            }
        }

        private HashSet<GitCommit> CollectCommits()
        {
            var set = new HashSet<GitCommit>();
            var stack = new Stack<GitCommit>();
            foreach (var branch in m_refs)
            {
                if (branch.Commit != null)
                    stack.Push(branch.Commit);
                else if (branch.Tag != null)
                    stack.Push(branch.Tag.Commit);
            }

            while (stack.Count > 0)
            {
                var c = stack.Pop();
                if (set.Add(c) && c.Parents != null)
                {
                    foreach (var p in c.Parents)
                    {
                        stack.Push(p);
                    }
                }
            }
            return set;
        }

        private void SaveTree(GitTree tree)
        {
            if (tree.Saved) 
                return;

            foreach (var e in tree.Entries)
            {
                var child = e.Object as GitTree;
                if (child != null)
                    SaveTree(child);
            }
            tree.Save(this);
        }

        private GitObject ReadGitObject(string sha1)
        {
            var path = Path.Combine(m_objectPath, sha1.Substring(0, 2), sha1.Substring(2));
            if (!File.Exists(path))
            {
                Console.WriteLine("Unknown reference: [{0}]", sha1);
                return null;
            }

            using (var file = new ZlibStream(File.OpenRead(path), CompressionMode.Decompress))
            {
                var typeAndLen = Utils.ReadString(file).Split(' ');
                var type = typeAndLen[0];
                var length = long.Parse(typeAndLen[1]);

                switch (type)
                {
                    case "blob": return new GitBlob(sha1);
                    case "tree": return new GitTree(sha1, length, file);
                    case "commit": return new GitCommit(sha1, length, file);
                    case "tag": return new GitTag(sha1, length, file);
                    default:
                        throw new Exception("Unknown git object: " + type);
                }
            }
        }

        internal byte[] LoadBlob(string sha1)
        {
            var path = Path.Combine(m_objectPath, sha1.Substring(0, 2), sha1.Substring(2));
            if (!File.Exists(path))
            {
                Console.WriteLine("Unknown reference: [{0}]", sha1);
                return null;
            }

            using (var file = new ZlibStream(File.OpenRead(path), CompressionMode.Decompress))
            {
                var typeAndLen = Utils.ReadString(file).Split(' ');
                if (typeAndLen[0] == "blob")
                {
                    var length = long.Parse(typeAndLen[1]);
                    var data = new byte[length];
                    file.Read(data, 0, (int)length);
                    return data;
                }
            }
            return null;
        }

        internal string WriteObject(string name, byte[] data)
        {
            var header = Encoding.ASCII.GetBytes(string.Format("{0} {1}", name, data.Length));
            var buffer = new byte[header.Length + data.Length + 1];
            Array.Copy(header, 0, buffer, 0, header.Length);
            Array.Copy(data, 0, buffer, header.Length + 1, data.Length);
            var sha1 = Utils.ToHex(s_sha1.ComputeHash(buffer));

            var path = Path.Combine(m_objectPath, sha1.Substring(0, 2), sha1.Substring(2));
            if (!File.Exists(path))
            {
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (var file = new ZlibStream(File.Create(path), CompressionMode.Compress, CompressionLevel.Level9))
                {
                    file.Write(buffer, 0, buffer.Length);
                    file.Flush();
                }
            }

            return sha1;
        }

        internal void WriteLFS(string sha256, byte[] data)
        {
            var path = Path.Combine(m_lfsPath, sha256.Substring(0, 2), sha256.Substring(2, 2), sha256);
            if (!File.Exists(path))
            {
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (var file = File.Create(path))
                {
                    file.Write(data, 0, data.Length);
                    file.Flush();
                }
            }
        }
    }
}
