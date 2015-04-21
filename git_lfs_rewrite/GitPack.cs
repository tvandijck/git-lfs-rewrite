using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace git_lfs_rewrite
{
    class GitPack
    {
        // index.
        private int[] m_fanOut;
        private string[] m_sha;
        private uint[] m_crc;
        private uint[] m_offset;

        public GitPack(string filename)
        {
            var index = Path.ChangeExtension(filename, ".idx");
            using (var file = new BinaryReader(File.OpenRead(index)))
            {
                // version 2.0 index?
                var header = (uint)IPAddress.NetworkToHostOrder(file.ReadInt32());
                if (header == 0xff744f63)
                {
                    int version = IPAddress.NetworkToHostOrder(file.ReadInt32());

                    // fan out table.
                    m_fanOut = new int[256];
                    for (int i = 0; i < 256; ++i)
                    {
                        m_fanOut[i] = IPAddress.NetworkToHostOrder(file.ReadInt32());
                    }

                    int objCount = m_fanOut[255];

                    m_sha = new string[objCount];
                    m_crc = new uint[objCount];
                    m_offset = new uint[objCount];

                    byte[] sha = new byte[20];
                    for (int i = 0; i < objCount; ++i)
                    {
                        file.Read(sha, 0, 20);
                        m_sha[i] = Utils.ToHex(sha);
                    }

                    for (int i = 0; i < objCount; ++i)
                    {
                        m_crc[i] = (uint)IPAddress.NetworkToHostOrder(file.ReadInt32());
                    }

                    for (int i = 0; i < objCount; ++i)
                    {
                        m_offset[i] = (uint)IPAddress.NetworkToHostOrder(file.ReadInt32());
                    }
                }
                else
                {
                    throw new Exception("Unsupported index format");
                }
            }
        }
    }
}
