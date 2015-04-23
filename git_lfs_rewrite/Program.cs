using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zlib;

namespace git_lfs_rewrite
{
    class Program
    {
        private static readonly SHA256 s_sha256 = new SHA256Managed();
        private static readonly HashSet<string> s_extentions = new HashSet<string> {
            ".exe",
            ".lib",
            ".a",
            "*.mp3",
            "*.zip",
            "*.dll",
            "*.pdb",
            "*.png",
            "*.bmp",
            "*.jpg",
            "*.pdf",
            "*.ico",
            "*.suo",
            "*.max",
            "*.com",
            "*.gif",
            "*.chm",
            "*.pch",
            "*.idb",
            "*.db",
            "*.bin",
            "*.dat",
            "*.dds",
            "*.ttf",
            "*.ppm",
            "*.dylib",
            "*.so",
            "*.msi",
            "*.bundle",
            "*.wav",
            "*.obj"
        };

        public static void MakeLFS(GitBlob blob, GitRepository repo)
        {
            lock (blob)
            {
                // see if it is already an LFS file.
                var data = repo.LoadBlob(blob.SHA1);
                if (data.Length < 200)
                {
                    var str = Encoding.ASCII.GetString(data);
                    if (str.Contains("version") && str.Contains("oid"))
                        return;
                }

                var sha256 = Utils.ToHex(s_sha256.ComputeHash(data));
                repo.WriteLFS(sha256, data);

                var sb = new StringBuilder();
                sb.Append("version https://git-lfs.github.com/spec/v1\n");
                sb.AppendFormat("oid sha256:{0}\n", sha256);
                sb.AppendFormat("size {0}\n", data.Length);
                var newData = Encoding.ASCII.GetBytes(sb.ToString());
                blob.SHA1 = repo.WriteObject("blob", newData);
            }
        }

        public static void MakeLFS(GitTree tree, GitRepository repo)
        {
            foreach (var e in tree.Entries)
            {
                // can't fix null objects.
                if (e.Object == null)
                    continue;

                // only process files.
                if ((e.Mode & 0x100000) == 0x100000 && s_extentions.Contains(Path.GetExtension(e.Name)))
                {
                    // update the object.
                    var blob = e.Object as GitBlob;
                    if (blob != null)
                    {
                        MakeLFS(blob, repo);
                    }
                }
            }
        }

        public static byte[] MakeAttributes()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ext in s_extentions)
            {
                sb.AppendFormat("*{0} filter=lfs -crlf\n", ext);
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static GitCommit InjectAttributes(GitRepository repo)
        {
            // create a new blob for the .attributes file.
            var attribSha1 = repo.WriteObject("blob", MakeAttributes());
            var blob = new GitBlob(attribSha1);
            var entry = new GitTree.Entry()
            {
                Mode = 0x100644,
                Name = ".gitattributes",
                ObjectHash = "",
                Object = blob
            };
            var tree = new GitTree(new[] { entry });
            var commit = new GitCommit(tree,
                "lfs-rewrite <lfs-rewrite@blizzard.com> 1380309170 -0400",
                "lfs-rewrite <lfs-rewrite@blizzard.com> 1380309170 -0400", 
                "\nadded .gitattributes");

            // add new commit to all commits that have no parent, or add the entry to the tree.
            foreach (var obj in repo.All())
            {
                var c = obj as GitCommit;
                if (c != null)
                {
                    if (c.Parents == null)
                    {
                        c.AddParent(commit);
                    }
                    else
                    {
                        c.Tree.Add(entry);
                    }
                }
            }

            return commit;
        }

        static void Main(string[] args)
        {
            var repository = new GitRepository("C:\\dev\\premake-lfs\\.git");

            // process the repository to make LFS objects where needed.
            Console.WriteLine("LFS...");
            foreach (var obj in repository.All())
            {
                var tree = obj as GitTree;
                if (tree != null)
                {
                    MakeLFS(tree, repository);
                }
            }

            // inject .gitattribute file
            InjectAttributes(repository);

            // save repository.
            repository.Save();


            //new GitPack("C:\\dev\\premake\\.git\\objects\\pack\\pack-b5d49a1bf368d1477052da79519760ba64aeb922.pack");
        }
    }
}
