using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Management;

namespace archiver
{
    public enum enumOperationType : int { eotUnknown = 0, eotCompress = 1, eotDecompress = 2 };
     
    

    public class clsArchiver
    {
        public const int DefaultBlockSize = 1048576;
        public enumOperationType OperationType = enumOperationType.eotUnknown;
        clsFileSource       mFileSource;
        clsFileDestination  mFileDestination;
        public int iBlockSize = DefaultBlockSize;


        List<string> ErrMsgs;

        public clsArchiver()
        {
            init();
        }

        public clsArchiver(string filein, string fileout, enumOperationType operationtype = enumOperationType.eotUnknown, int iblockSize = DefaultBlockSize)
        {
            init();
            FileSource.FileName = filein;
            FileDestination.FileName = fileout;
            OperationType = operationtype;
            iBlockSize = iblockSize;
        }
        /// <summary>
        /// Инициация внутренних экземпляров класов
        /// </summary>
        void init()
        {
            FileSource = new clsFileSource();
            FileDestination = new clsFileDestination();
            ErrMsgs = new List<string>();
        }

        public clsFileSource FileSource
        {
            get { return mFileSource; }
            set { mFileSource = value; }
        }

        public string FileSourceName
        {
            get { return FileSource.FileName; }
            set { FileSource.FileName = value; }
        }

        public clsFileDestination FileDestination
        {
            get { return mFileDestination; }
            set { mFileDestination = value; }
        }

        public string FileDestinationName
        {
            get { return FileDestination.FileName; }
            set { FileDestination.FileName = value; }
        }

        /// <summary>
        /// Проверка входных параметров, если проверка не прошла, процедура запишит ошибки в  ErrMsgs
        /// </summary>
        /// <returns>false - проверка не прошла, true - все хорошо</returns>
        public bool CheckParameters()
        {
            bool ProcessResult = false;
            if (OperationType == enumOperationType.eotUnknown)
            {
                ProcessResult = false;
                ErrMsgs.Add("Некорректная операция");
            }
            else
            {

                    if (FileSource.CheckFile() == enumCheckFileResult.ecfrFileIsFound) //наличие файла источника
                    {
                        long FileSize = FileSource.FileSize;
                        
                        if ((iBlockSize > 0 && iBlockSize < DefaultBlockSize) && FileSize > DefaultBlockSize) //проверка размера блока
                        {
                            Console.WriteLine("Указаный размер блока меньше размера блока по умолчанию, в некоторых ситуациях это может сказаться на времени архивации и на итоговом размере, вы хотите продолжить [Y] с указаным размером блока или использовать размер блока по умолчанию [N]?");
                            Console.WriteLine("Y/N?");

                            string answer = Console.ReadLine();
                            Regex rStr = new Regex("^(?<value>[nN])");
                            MatchCollection coll;
                            coll = rStr.Matches((answer));
                            if (coll.Count > 0)
                            {
                                iBlockSize = DefaultBlockSize;
                            }
                            else
                            {
                                rStr = new Regex("^(?<value>[yY])");
                                coll = rStr.Matches((answer));
                                if (coll.Count == 0)
                                    iBlockSize = DefaultBlockSize;
                            }
                        }
                        else if (iBlockSize == 0)
                            iBlockSize = DefaultBlockSize;

                        
                        enumCheckFileResult dstCheck = FileDestination.CheckFile(); //проверка файла назначения

                        if (dstCheck == enumCheckFileResult.ecfrMissingFileName) //забыли указать имя
                        {
                            FileDestination.FileName = FileSource.FileName + (OperationType == enumOperationType.eotCompress ? ".gz" : ".ungz");
                            dstCheck = FileDestination.CheckFile();
                        }

                        if (dstCheck == enumCheckFileResult.ecfrPathNotFound) //
                        {
                            ProcessResult = false;
                            ErrMsgs.Add("Директория назначения отстутствует");
                        }
                        else if (dstCheck == enumCheckFileResult.ecfrFileIsFound)
                        {
                            Console.WriteLine("Файл назначения уже есть, перезаписать?");
                            Console.WriteLine("Y/N?");
                            string answer = Console.ReadLine();
                            Regex rStr = new Regex("^(?<value>[yY])");
                            MatchCollection coll;
                            coll = rStr.Matches((answer));
                            if (coll.Count > 0)
                            {
                                FileDestination.ReWrite = true;
                                ProcessResult = true;
                            }
                            else
                                ProcessResult = false;
                        }
                        else if (dstCheck == enumCheckFileResult.ecfrPathIsFound) //путь назначения присутствует
                        {
                            ProcessResult = true;
                        }


                        if (ProcessResult == true) //параметры файла назначения вроде как прошли, нужно ещё кое что проверить
                        {

                            // здесь идет проверка на свободное место,  за конечный размер файла взят размер источника, т.к. с оптимизмом предположил что размер файла назначения будет меньше или равен файлу источника.
                            //!!!Это замечание справедливо для процесса архивирования, для разархивирования нужно проверку делать по другому, как, пока не знаю

                            if (clsFileBase.GetDriveFreeSpace(clsFileBase.GetDriveName(FileDestination.FileName)) > FileSize)
                                ProcessResult = true;
                            else
                            {
                                ProcessResult = false;
                                ErrMsgs.Add("Расчетный размер файла может превысить свободное место на целевом диске");
                            }
                        }
                    }
                    else
                        ErrMsgs.Add(FileSource.CheckMsg);
                
            }
            return ProcessResult;
        }
        public bool ProcessFile()
        {
            bool ProcessResult = false;

            switch (OperationType)
            {
                case enumOperationType.eotCompress:
                    ProcessResult = Compress();
                    break;
                case enumOperationType.eotDecompress:
                    ProcessResult = Decompress();
                    break;
                default:
                    ErrMsgs.Add("Не определена операция");
                    ProcessResult = false;
                    break;
            }

            //if (ProcessResult == false)
            //    ShowErrs();

            return ProcessResult;
        }

        /// <summary>
        /// Процедура инициирует экземляр класса архиватора, передает ему параметры и запускает процедуру архивации
        /// все возникшие ошибки будут записаны в ErrMsgs
        /// </summary>
        /// <returns>false - завершилось с ошибками, true - все хорошо</returns>
        public bool Compress()
        {
            bool ProcessResult = false;
            Console.WriteLine("Compressing...");

            mFileSource.FileOpen();
            mFileDestination.FileOpen();

            //clsGZipProcessor GZPrc = new clsGZipProcessor();
            clsGZipProcessor1 GZPrc = new clsGZipProcessor1();
            GZPrc.iBlockSize = iBlockSize;
            //ProcessResult = GZPrc.Compress(mFileSource.fStream, mFileDestination.fStream); //clsGZipProcessor
            ProcessResult = GZPrc.Compress(mFileSource, mFileDestination); //clsGZipProcessor1 

            mFileSource.FileClose();
            mFileDestination.FileClose();

            if (ProcessResult == false)
                ErrMsgs.Add(GZPrc.ErrMessage);

            

            return ProcessResult;
        }

        /// <summary>
        /// Процедура инициирует экземляр класса архиватора, передает ему параметры и запускает процедуру разорхивации
        /// все возникшие ошибки будут записаны в ErrMsgs
        /// </summary>
        /// <returns>false - завершилось с ошибками, true - все хорошо</returns>

        public bool Decompress()
        {
            bool ProcessResult = false;
            Console.WriteLine("Decompressing...");

            mFileSource.FileOpen();
            mFileDestination.FileOpen();

            //clsGZipProcessor GZPrc = new clsGZipProcessor();
            //ProcessResult = GZPrc.Decompress(mFileSource.fStream, mFileDestination.fStream); //clsGZipProcessor

            clsGZipProcessor1 GZPrc = new clsGZipProcessor1();
            ProcessResult = GZPrc.Decompress(mFileSource, mFileDestination);//clsGZipProcessor1 

            mFileSource.FileClose();
            mFileDestination.FileClose();

            if (ProcessResult == false)
                ErrMsgs.Add(GZPrc.ErrMessage);

            return ProcessResult;
        }

        /// <summary>
        /// Процедура выводит в консоль перечень возникших ошибок
        /// </summary>
        public void ShowErrs()
        {

            if (ErrMsgs.Count > 0)
            {
                Console.WriteLine("Ошибки:");
                Console.WriteLine("");

                for (int i = 0; i < ErrMsgs.Count; i++)
                {
                    Console.WriteLine(ErrMsgs[i]);
                }
                Console.WriteLine("");
            }
        }

        static long GetMemFreeSize()
        {

            long FreeRam =0 ;

            ManagementObjectSearcher ramMonitor =    //запрос к WMI для получения памяти ПК
                new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem");

            foreach (ManagementObject objram in ramMonitor.Get())
            {
                FreeRam = Convert.ToInt64(objram["FreePhysicalMemory"]);
            }

            return FreeRam;
        }

    }
}
