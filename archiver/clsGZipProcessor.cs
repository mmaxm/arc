using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace archiver
{
    public class clsGZipProcessor
    {
        List<clsArcThread> ArcThreads; //Список для хранения потоков и данных для обработки
        public int iBlockSize = 1048576;
        public string ErrMessage = "";

        public clsGZipProcessor()
        {
            ArcThreads = new List<clsArcThread>();
        }
        int GetBlockSize(int BlockSize, long RestFile)
        {
            int iblockSize=0;

            if (BlockSize == 0)
            {
                if (RestFile > int.MaxValue)
                    iblockSize = int.MaxValue;
                else
                    iblockSize = (int)RestFile;
            }
            else if (BlockSize > (int)RestFile)
                    iblockSize = (int)RestFile;
            else
                iblockSize = BlockSize;

            return iblockSize;

        }
        /// <summary>
        /// Процедура упаковки фалов
        /// </summary>
        /// <param name="inFile">Поток с содержимое входящего файла</param>
        /// <param name="outFile">Поток, куда писать результат</param>
        /// <returns></returns>
        public bool Compress(FileStream inFile, FileStream outFile)
        {
            bool ProcessResult = false;
            try
            {
                if (inFile != null && outFile != null)
                {
                    int _iBlockSize = GetBlockSize(iBlockSize, inFile.Length - inFile.Position);
                    //int threadNumber = (iBlockSize == 0 ? 1 : (int)(inFile.Length / iBlockSize) + (int)((inFile.Length % iBlockSize) > 0 ? 1 : 0));
                    int threadNumber = (int)(inFile.Length / _iBlockSize) + (int)((inFile.Length % _iBlockSize) > 0 ? 1 : 0);

                    
                    //В цикле считываем поблочно файл и полученные блоки записываем в clsArcThread, для последующего сжатия, инициируем поток и запускаем на выполнение
                    for (int BlockCount = 0;
                         (BlockCount < threadNumber) && 
                          (inFile.Position < inFile.Length);
                          BlockCount++)

                    //while ((inFile.Position < inFile.Length))
                    {
                        

                        //if (iBlockSize == 0)
                        //    _iBlockSize = (int)(inFile.Length - inFile.Position);
                        //else if (inFile.Length - inFile.Position <= iBlockSize)
                        //    _iBlockSize = (int)(inFile.Length - inFile.Position);
                        //else
                        //    _iBlockSize = iBlockSize;

                        ArcThreads.Add(new clsArcThread());
                        //ArcThreads[BlockCount].ThreadNumber = BlockCount;
                        ArcThreads[BlockCount].DataArray = new byte[_iBlockSize];

                        inFile.Read(ArcThreads[BlockCount].DataArray, 0, _iBlockSize);

                        ArcThreads[BlockCount].wrkThread = new Thread(CompressBlock);
                        ArcThreads[BlockCount].wrkThread.Start(BlockCount);
                        
                        _iBlockSize = GetBlockSize(iBlockSize, inFile.Length - inFile.Position);


                    }

                    //В цикле проверяем потоки на завершение и последовательно записываем упаковыные блоки в файл
                    for (int portionCount = 0; (portionCount < ArcThreads.Count); )
                    {
                        if (ArcThreads[portionCount].wrkThread.ThreadState == ThreadState.Stopped)
                        {
                            BitConverter.GetBytes(ArcThreads[portionCount].CompressedDataArray.Length + 1)
                                        .CopyTo(ArcThreads[portionCount].CompressedDataArray, 4);
                            outFile.Write(ArcThreads[portionCount].CompressedDataArray, 0, ArcThreads[portionCount].CompressedDataArray.Length);

                            ArcThreads[portionCount] = null;
                            System.GC.Collect();

                            portionCount++;
                        }
                    }
                    ProcessResult = true;
                }

            }
            catch (Exception ex)
            {
                //Console.WriteLine("ERROR:" + ex.Message);
                ErrMessage = "ERROR:" + ex.Message;
                ProcessResult = false;
            }

            return ProcessResult;
        }
        /// <summary>
        /// Процедура упаковки блока, обращается clsArcThread, выбирает исходный блок и упаковывает его
        /// </summary>
        /// <param name="i">Содержит номер в списке ArcThreads</param>
        public void CompressBlock(object i)
        {
            using (MemoryStream output = new MemoryStream(ArcThreads[(int)i].DataArray.Length))
            {
                using (GZipStream cs = new GZipStream(output, CompressionMode.Compress))
                {
                    cs.Write(ArcThreads[(int)i].DataArray, 0, ArcThreads[(int)i].DataArray.Length);
                }
                ArcThreads[(int)i].CompressedDataArray = output.ToArray();
                ArcThreads[(int)i].DataArray = null;
            }
        }

        /// <summary>
        /// Процедура распаковки файла
        /// </summary>
        /// <param name="inFile">Поток с архивом</param>
        /// <param name="outFile">Поток куда писать распокованые данные</param>
        /// <returns></returns>
        public bool Decompress(FileStream inFile, FileStream outFile)
        {
            bool ProcessResult = false;

            try
            {
                if (inFile != null && outFile != null)
                {
                    int _iBlockSize; //размер распокованого блока, который нужно записать
                    int compressedBlockLength; //размер упакованого блока, который необходимо считать

                    //Console.Write("Decompressing...");
                    byte[] buffer = new byte[8];


                    //поблочно считываем сжатые файлы и запускаем процесс распаковки
                    for (int BlockCount = 0; inFile.Position < inFile.Length; BlockCount++)
                    {

                        ArcThreads.Add(new clsArcThread());
                       // Console.Write(".");
                        //считываем начало блока, чтобы определить его размер, если вернули 0, значит файл сжат одним блоком
                        inFile.Read(buffer, 0, 8);
                        compressedBlockLength = BitConverter.ToInt32(buffer, 4);
                        if (compressedBlockLength <= 0)
                            compressedBlockLength = (int)inFile.Length;
                        else
                            compressedBlockLength = compressedBlockLength - 1;

                        //записываем упакованый блок в массив, для последующей распаковки, размер масива равен compressedBlockLength
                        ArcThreads[BlockCount].CompressedDataArray = new byte[compressedBlockLength];
                        buffer.CopyTo(ArcThreads[BlockCount].CompressedDataArray, 0);
                        inFile.Read(ArcThreads[BlockCount].CompressedDataArray, 8, compressedBlockLength - 8);
                        //считываем в конце архивного блока размер не сжатого блока и изменяем размер массива для несжатых данных
                        _iBlockSize = BitConverter.ToInt32(ArcThreads[BlockCount].CompressedDataArray, compressedBlockLength - 4);
                        ArcThreads[BlockCount].DataArray = new byte[_iBlockSize];

                        ArcThreads[BlockCount].wrkThread = new Thread(DecompressBlock);
                        ArcThreads[BlockCount].wrkThread.Start(BlockCount);

                        
                    }

                    //В цикле проверяем потоки на завершение и последовательно записываем распаковыные блоки в файл
                    for (int portionCount = 0; (portionCount < ArcThreads.Count); )
                    {
                        if (ArcThreads[portionCount].wrkThread.ThreadState == ThreadState.Stopped)
                        {
                            outFile.Write(ArcThreads[portionCount].DataArray, 0, ArcThreads[portionCount].DataArray.Length);
                            portionCount++;
                        }
                    }

                    ProcessResult = true;

                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("ERROR:" + ex.Message);
                ErrMessage = "ERROR:" + ex.Message;
                ProcessResult =  false;
            }

            return ProcessResult;

        }

        /// <summary>
        /// Процедура распаковки блока, обращается clsArcThread, выбирает исходный блок и упаковывает его
        /// </summary>
        /// <param name="i">Содержит номер в списке ArcThreads</param>
        public void DecompressBlock(object i)
        {
            using (MemoryStream input = new MemoryStream(ArcThreads[(int)i].CompressedDataArray))
            {

                using (GZipStream ds = new GZipStream(input, CompressionMode.Decompress))
                {
                    ds.Read(ArcThreads[(int)i].DataArray, 0, ArcThreads[(int)i].DataArray.Length);
                    ArcThreads[(int)i].CompressedDataArray = null;
                }

            }
        }

    }
}
