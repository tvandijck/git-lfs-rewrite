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

        static void Main(string[] args)
        {
            var repository = new GitRepository("C:\\dev\\sc2-git");

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

            repository.Save();


            //new GitPack("C:\\dev\\premake\\.git\\objects\\pack\\pack-b5d49a1bf368d1477052da79519760ba64aeb922.pack");
        }
    }
}
