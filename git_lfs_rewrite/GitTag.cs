using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace git_lfs_rewrite
{
    class GitTag : GitObject
    {
        private string m_hash;
        private GitCommit m_commit;
        private readonly string m_type;
        private readonly string m_tag;
        private readonly string m_tagger;

        public GitTag(string sha1, long length, Stream data)
            : base(sha1)
        {
            var temp = new byte[length];
            data.Read(temp, 0, (int)length);
            var str = Encoding.UTF8.GetString(temp);

            foreach (var line in str.Split('\n'))
            {
                if (line.StartsWith("object"))
                {
                    m_hash = line.Substring(7);
                }
                else if (line.StartsWith("type"))
                {
                    m_type = line.Substring(5);
                }
                else if (line.StartsWith("tagger"))
                {
                    m_tagger = line.Substring(7);
                }
                else if (line.StartsWith("tag"))
                {
                    m_tag = line.Substring(4);
                }
            }
        }

        public GitCommit Commit
        {
            get { return m_commit; }
        }

        public override void Resolve(GitRepository repo)
        {
            if (!string.IsNullOrEmpty(m_hash))
                m_commit = (GitCommit)repo.GetObject(m_hash);
        }

        public override bool Save(GitRepository repo)
        {
            m_saved = true;

            if (!IsDirty())
                return false;

            m_hash = m_commit.SHA1;

            var sb = new StringBuilder();
            sb.AppendFormat("object {0}\n", m_hash);
            sb.AppendFormat("type {0}\n", m_type);
            sb.AppendFormat("tag {0}\n", m_tag);
            sb.AppendFormat("tagger {0}\n\n", m_tagger);

            var data = Encoding.UTF8.GetBytes(sb.ToString());
            SHA1 = repo.WriteObject("tag", data);

            return true;
        }

        private bool IsDirty()
        {
            return (m_commit != null && m_commit.SHA1 != m_hash);
        }
    }
}
