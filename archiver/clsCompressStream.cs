using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.IO.Compression;

namespace archiver
{
    public class clsCompressStream
    {
        public byte[] SrcByteArr;
        public clsFileDestination DstFile;
        public int ThreadIndex;
        public int iBlockSize;
        public Thread thread;


        public clsCompressStream()
        {
            
        }

        public void StartCompress()
        {
            thread = new Thread(this.CompressStream);
            thread.Name = String.Format("name {0}", ThreadIndex);
            thread.Start();
        }

        public void StartDecompress()
        {
            thread = new Thread(this.DecompressStream);
            thread.Name = String.Format("name {0}", ThreadIndex);
            thread.Start();
        }

       

        public void CompressStream()
        {
            if (SrcByteArr.Length > 0 && DstFile != null)
            {
                using (MemoryStream output = new MemoryStream(SrcByteArr.Length))
                {
                    using (GZipStream cs = new GZipStream(output,   CompressionMode.Compress ))
                    {
                        cs.Write(SrcByteArr, 0, SrcByteArr.Length);
                        SrcByteArr = null;
                    }

                    bool resproc = false;

                    while (resproc == false)
                    {
                        lock (DstFile)
                        {
                            if (DstFile.NumLastProceded == ThreadIndex - 1)
                            {

                                byte[] DstByteArr = output.ToArray();
                                BitConverter.GetBytes(DstByteArr.Length + 1).CopyTo(DstByteArr, 4);
                                DstFile.fStream.Write(DstByteArr, 0, DstByteArr.Length);
                                DstFile.NumLastProceded = ThreadIndex;
                                DstByteArr = null;
                                resproc = true;
                                //Console.Write("{0}", ThreadIndex);
                                Console.Write(".");

                            }
                        }
                    }
                }
                
            }
        }

  


        public void DecompressStream()

        {
            if (SrcByteArr.Length > 0 && DstFile != null)
            {
                byte[] DstByteArr = new byte[iBlockSize];
                using (MemoryStream input = new MemoryStream(SrcByteArr))
                {
                    using (GZipStream ds = new GZipStream(input, CompressionMode.Decompress))
                    {
                        ds.Read(DstByteArr, 0, DstByteArr.Length);
                        SrcByteArr = null;
                    }

                    bool resproc = false;
                    while (resproc == false)
                    {
                        lock (DstFile)
                        {
                            if (DstFile.NumLastProceded == ThreadIndex - 1)
                            {
                                DstFile.fStream.Write(DstByteArr, 0, DstByteArr.Length);
                                DstFile.NumLastProceded = ThreadIndex;
                                DstByteArr = null;
                                resproc = true;
                                //Console.Write("{0}", ThreadIndex);
                                Console.Write(".");
                            }
                        }
                    }

                }
            }
        }
    }
}