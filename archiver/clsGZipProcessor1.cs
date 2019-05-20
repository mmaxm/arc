using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.IO.Compression;
using System.Threading;

namespace archiver
{

    public class clsGZipProcessor1
    {
        List<clsCompressStream> ArcThreads; //Список для хранения потоков и данных для обработки
        public int iBlockSize = 1048576;
        
        /// <summary>
        /// MaxMemoryUse необходима для того, чтобы при достяжении данной планки по использованию памяти временно приостанавливать создания новых процессов пока не освободится память, подобрана опытным путем, решение немного не нравится но на данный момент другого не нашел
        /// </summary>
        public int MaxMemoryUse = 536870912;
        public string ErrMessage = "";

        public clsGZipProcessor1()
        {
            ArcThreads = new List<clsCompressStream>();
        }
        /// <summary>
        /// Процедура возвращает размер блока для последующей упаковки, если не достигли конца файла, то возращает установленный, если приблизились , возращает остаток
        /// </summary>
        /// <param name="BlockSize">То что заказали</param>
        /// <param name="RestFile">Остаток файла</param>
        /// <returns></returns>
        int GetBlockSize(int BlockSize, long RestFile)
        {
            int iblockSize = 0;

            //на тот случай если придет нулевой блок
            if (BlockSize == 0)
            {
                if (RestFile > MaxMemoryUse)
                    iblockSize = MaxMemoryUse/4;
                else
                    iblockSize = (int)RestFile;
            }
            else if ((long)BlockSize > RestFile)
                iblockSize = (int)RestFile;
            else
                iblockSize = BlockSize;

            return iblockSize;

        }
        /// <summary>
        /// Упоковка файла
        /// </summary>
        /// <param name="inFile">Файл, который нужно завоковать</param>
        /// <param name="outFile"><Файл, куда запаковать/param>
        /// <returns>Возвращает true если успешно, false если возникла ошибка и нужно выве сообщение об ошибке</returns>
        public bool Compress(clsFileSource inFile, clsFileDestination outFile)
        {
            bool ProcessResult = false;
            try
            {
                if (inFile != null && outFile != null)
                {
                    int _iBlockSize = GetBlockSize(iBlockSize, inFile.fStream.Length - inFile.fStream.Position);
                    int threadNumber = (int)(inFile.fStream.Length / _iBlockSize) + (int)((inFile.fStream.Length % _iBlockSize) > 0 ? 1 : 0);

                    for (int BlockCount = 0;
                         (BlockCount < threadNumber) &&
                          (inFile.fStream.Position < inFile.fStream.Length);
                          )
                    {
                        //проверка на превышение используемой памяти, если не превысили, запускаем очередной процесс упаковки блока
                        if ( System.Environment.WorkingSet < MaxMemoryUse - (_iBlockSize))
                        {
                            ArcThreads.Add(new clsCompressStream());

                            ArcThreads[BlockCount].ThreadIndex = BlockCount;
                            ArcThreads[BlockCount].DstFile = outFile;
                            ArcThreads[BlockCount].SrcByteArr = new byte[_iBlockSize];
                            inFile.fStream.Read(ArcThreads[BlockCount].SrcByteArr, 0, _iBlockSize);
                            ArcThreads[BlockCount].StartCompress();
                            BlockCount++;
                          //  Console.WriteLine(System.Environment.WorkingSet.ToString());

                            _iBlockSize = GetBlockSize(iBlockSize, inFile.fStream.Length - inFile.fStream.Position);
                            clearThread();
                        }
                        else //если привысили, запускаем процедуру подчистки памяти 
                        {

                           // Console.WriteLine(string.Format(">>{0}",System.Environment.WorkingSet.ToString()));
                            clearThread();
                        }
                    }

                    //Ждем окончание всех процессов и подчищаем память, необходимо чтобы все процессы успели записать свои блоки в файл
                    while (clearThread() == false)
                    {
                        
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

        bool clearThread()
        {
            bool AllThreadsStop = true;
            //Console.WriteLine(System.GC.GetTotalMemory(false).ToString());

            for (int i = 0; i < ArcThreads.Count; i++)
            {
                if (ArcThreads[i] != null)
                {
                    if (ArcThreads[i].thread.ThreadState == ThreadState.Stopped)
                    {
                        ArcThreads[i] = null;
                    }
                    else
                        AllThreadsStop = false;
                }
            }
            System.GC.Collect();

            return AllThreadsStop;
        }

        /// <summary>
        /// Процедура распаковки файла
        /// </summary>
        /// <param name="inFile">Архив для распаковки</param>
        /// <param name="outFile">Куда распаковывать</param>
        /// <returns>Возвращает true если успешно, false если возникла ошибка и нужно выве сообщение об ошибке</returns>
        public bool Decompress(clsFileSource inFile, clsFileDestination outFile)
        {

            bool ProcessResult = false;
            try
            {
                //int _iBlockSize; //размер распокованого блока, который нужно записать
                int compressedBlockLength; //размер упакованого блока, который необходимо считать

                //Console.Write("Decompressing...");
                byte[] buffer = new byte[8];

                //поблочно считываем сжатые файлы и запускаем процесс распаковки
                for (int BlockCount = 0; inFile.fStream.Position < inFile.fStream.Length; )
                {
                    //проверка на превышение используемой памяти, если не превысили, запускаем очередной процесс распаковки блока
                    if (System.Environment.WorkingSet < MaxMemoryUse )
                    {
                        ArcThreads.Add(new clsCompressStream());
                        //выясняем, какой блок необходимо считать из файла, чтобы отдать процесу на распоковку
                        inFile.fStream.Read(buffer, 0, 8);
                        compressedBlockLength = BitConverter.ToInt32(buffer, 4);
                        if (compressedBlockLength <= 0)
                            compressedBlockLength = (int)inFile.fStream.Length;
                        else
                            compressedBlockLength = compressedBlockLength - 1;
                       
                        ArcThreads[BlockCount].DstFile = outFile;
                        //записываем блок в масив
                        ArcThreads[BlockCount].SrcByteArr = new byte[compressedBlockLength];
                        buffer.CopyTo(ArcThreads[BlockCount].SrcByteArr, 0);
                        inFile.fStream.Read(ArcThreads[BlockCount].SrcByteArr, 8, compressedBlockLength - 8);
                        
                        //Выясняем размер целевого блока
                        ArcThreads[BlockCount].iBlockSize = BitConverter.ToInt32(ArcThreads[BlockCount].SrcByteArr, compressedBlockLength - 4);
                        ArcThreads[BlockCount].ThreadIndex = BlockCount; //необходимо для записи блоков в правильном порядке
                        ArcThreads[BlockCount].StartDecompress();

                        BlockCount++;
                        clearThread();
                    }
                    else //если привысили, запускаем процедуру подчистки памяти
                    {
                        // Console.WriteLine(string.Format(">>{0}",System.Environment.WorkingSet.ToString()));
                        clearThread();
                    }
                }


                //Ждем окончание всех процессов
                while (clearThread() == false)
                {

                }

                ProcessResult = true;

            }
            catch (Exception ex)
            {
                //Console.WriteLine("ERROR:" + ex.Message);
                ErrMessage = "ERROR:" + ex.Message;
                ProcessResult = false;
            }

            return ProcessResult;
        }


    }
}
