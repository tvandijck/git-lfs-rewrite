using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace git_lfs_rewrite
{
    class GitBlob : GitObject
    {
        public GitBlob(string sha1)
            : base(sha1)
        {
        }

        public override void Resolve(GitRepository repo)
        {
        }

        public override bool Save(GitRepository repo)
        {
            m_saved = true;
            return false;
        }
    }
}
