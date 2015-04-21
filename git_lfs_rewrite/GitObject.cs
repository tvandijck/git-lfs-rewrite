using System.Collections.Generic;

namespace git_lfs_rewrite
{
    abstract class GitObject
    {
        private string m_sha1;
        protected bool m_saved;

        protected GitObject(string sha1)
        {
            m_sha1 = sha1;
        }

        public string SHA1
        {
            get { return m_sha1; }
            set { m_sha1 = value; }
        }

        public bool Saved
        {
            get { return m_saved; }
            set { m_saved = value; }
        }

        public abstract void Resolve(GitRepository repo);
        public abstract bool Save(GitRepository repo);
    }
}