using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace git_lfs_rewrite
{
    class GitCommit : GitObject
    {
        private string m_treeHash;
        private GitTree m_tree;
        private readonly List<string> m_parentHash;
        private List<GitCommit> m_parent;
        private readonly string m_author;
        private readonly string m_committer;
        private readonly string m_message;

        public GitCommit(string sha1, long length, Stream data)
            : base(sha1)
        {
            var temp = new byte[length];
            data.Read(temp, 0, (int)length);
            var str = Encoding.UTF8.GetString(temp);

            var lines = str.Split('\n');
            if (lines[0].StartsWith("tree "))
            {
                m_treeHash = lines[0].Substring(5);
            }

            var idx = 1;
            while (lines[idx].StartsWith("parent "))
            {
                if (m_parentHash == null)
                    m_parentHash = new List<string>();
                m_parentHash.Add(lines[idx].Substring(7));
                idx++;
            }

            if (lines[idx].StartsWith("author "))
            {
                m_author = lines[idx].Substring(7);
                idx++;
            }

            if (lines[idx].StartsWith("committer "))
            {
                m_committer = lines[idx].Substring(10);
                idx++;
            }

            var sb = new StringBuilder();
            for (var i = idx; i < lines.Length; ++i)
            {
                sb.Append(lines[i]);
                if (i < lines.Length - 1)
                    sb.Append("\n");
            }
            m_message = sb.ToString();
        }

        public GitTree Tree
        {
            get { return m_tree; }
        }

        public IEnumerable<GitCommit> Parents
        {
            get { return m_parent; }
        }

        public override void Resolve(GitRepository repo)
        {
            if (!string.IsNullOrEmpty(m_treeHash))
                m_tree = (GitTree)repo.GetObject(m_treeHash);
            if (m_parentHash != null)
            {
                m_parent = new List<GitCommit>();
                foreach (var p in m_parentHash)
                {
                    m_parent.Add((GitCommit)repo.GetObject(p));
                }
            }
        }

        public override bool Save(GitRepository repo)
        {
            m_saved = true;

            if (!IsDirty())
                return false;

            var sb = new StringBuilder();

            m_treeHash = m_tree.SHA1;
            sb.AppendFormat("tree {0}\n", m_treeHash);

            if (m_parent != null)
            {
                for (var i = 0; i < m_parent.Count; ++i)
                {
                    m_parentHash[i] = m_parent[i].SHA1;
                    sb.AppendFormat("parent {0}\n", m_parentHash[i]);
                }
            }

            sb.AppendFormat("author {0}\n", m_author);
            sb.AppendFormat("committer {0}\n", m_committer);
            sb.Append(m_message);

            var data = Encoding.UTF8.GetBytes(sb.ToString());
            SHA1 = repo.WriteObject("commit", data);

            return true;
        }

        private bool IsDirty()
        {
            if (m_tree != null && m_tree.SHA1 != m_treeHash)
                return true;

            if (m_parent != null)
            {
                for (var i = 0; i < m_parent.Count; ++i)
                {
                    if (m_parentHash[i] != m_parent[i].SHA1)
                        return true;
                }
            }
            return false;
        }
    }
}