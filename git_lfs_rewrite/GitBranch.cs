using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace git_lfs_rewrite
{
    class GitBranch : GitObject
    {
        private string m_hash;
        private GitCommit m_commit;
        private GitTag m_tag;
        private readonly string m_name;

        public GitBranch(string name, string sha1)
            : base(string.Empty)
        {
            m_name = name;
            m_hash = sha1;
        }

        public GitCommit Commit
        {
            get { return m_commit; }
        }

        public GitTag Tag
        {
            get { return m_tag; }
        }

        public string Hash
        {
            get { return m_hash; }
        }

        public string Name
        {
            get { return m_name; }
        }

        public override void Resolve(GitRepository repo)
        {
            if (!string.IsNullOrEmpty(m_hash))
            {
                var obj =repo.GetObject(m_hash);
                m_commit = obj as GitCommit;
                m_tag = obj as GitTag;
            }
        }

        public override bool Save(GitRepository repo)
        {
            m_saved = true;

            if (m_commit != null)
                m_hash = m_commit.SHA1;
            else
                m_hash = m_tag.SHA1;

            return false;
        }
    }
}
