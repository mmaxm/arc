using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace archiver
{
    public enum enumCheckFileResult : int { ecfrUnknown = 0, ecfrOk = 1, ecfrFileIsFound = 2, ecfrFileNotFound = 3, ecfrPathIsFound = 4, ecfrPathNotFound = 5, ecfrDriveNotFound = 6, ecfrDriveNoFreeSpace = 7, ecfrMissingFileName = 8 }

    /// <summary>
    /// Базовый класс для работы с файлами
    /// </summary>
    public class clsFileBase
    {

        public string CheckMsg = "";
        string sFileName = "";
        public FileStream fStream;
        public enumCheckFileResult ResultCheck = enumCheckFileResult.ecfrUnknown;
        protected Nullable<long> mFileSize = null;

        public clsFileBase()
        {
        }

        public clsFileBase(string filename)
        {
            FileName = filename;
        }

        public long FileSize
        {
            get
            {
                if (mFileSize.HasValue)
                    return mFileSize.Value;
                else
                {
                    if (CheckFile(sFileName) == enumCheckFileResult.ecfrFileIsFound)
                    {
                        System.IO.FileInfo file = new System.IO.FileInfo(sFileName);
                        mFileSize = file.Length;
                        return mFileSize.Value;
                    }
                    else
                        return 0;
                }
            }
        }

        public string FileName
        {
            get { return sFileName; }
            set
            {
                if (value.Length == 0 || value == null)
                {
                    ResultCheck = enumCheckFileResult.ecfrMissingFileName;
                    sFileName = "";
                }
                if (Path.IsPathRooted(value))
                    sFileName = value;
                else
                    sFileName = Path.Combine(CurrentDirectory, value); //если пользователь не указал полный путь к файлу, ищем его в текущей дериктории
            }
        }

        public void FileClose()
        {
            if (fStream != null)
                fStream.Close();
        }

        //Переопределяемая фунция проверки файла (наличие / отсутсвие) вызывает статичную функцию
        public virtual enumCheckFileResult CheckFile()
        {
            ResultCheck = CheckFile(FileName);
            return ResultCheck;
        }

        public virtual bool FileOpen()
        {
            return false;
        }

        

        public static enumCheckFileResult CheckFile(string filename)
        {
            enumCheckFileResult resultcheck = enumCheckFileResult.ecfrUnknown;
            if (filename.Length > 0)

                if (File.Exists(filename))
                {
                    resultcheck = enumCheckFileResult.ecfrFileIsFound;
                }
                else
                {
                    resultcheck = enumCheckFileResult.ecfrFileNotFound;
                }
            else
            {
                resultcheck = enumCheckFileResult.ecfrMissingFileName;
            }
            return resultcheck;
        }

        public static long GetDriveFreeSpace(string driveName)
        {
            long AvailableFreeSpace = 0;

            if (System.IO.Directory.Exists(driveName))
            {
                System.IO.DriveInfo Drv = new System.IO.DriveInfo(driveName);

                if (Drv != null) { AvailableFreeSpace = Drv.AvailableFreeSpace; }
            }

            return AvailableFreeSpace;

        }
        
        public static string GetDriveInfo(string driveName)
        {
            if (System.IO.Directory.Exists(driveName))
            {
                System.IO.DriveInfo Drv = new System.IO.DriveInfo(driveName);

                if (Drv != null)

                    return string.Format("На диске {0} доступно свободного места {1} свободное место {2}", Drv.Name, Drv.AvailableFreeSpace.ToString(), Drv.TotalFreeSpace.ToString());
                else
                    return "";
            }
            else
                return "Неверно указан диск";
        }

        public static string CurrentDirectory
        {
            get { return System.Environment.CurrentDirectory.ToString(); }
        }

        public static string GetDriveName(string filename)
        {
            System.IO.FileInfo file = new System.IO.FileInfo(filename);
            //Console.WriteLine(file.Directory.ToString());
            //Console.WriteLine(file.Directory.Root.Name.ToString());
            //file.Directory.Root.Name
            return file.Directory.Root.Name;
        }

        public static enumCheckFileResult CheckDirectory(string filename)
        {
            System.IO.FileInfo file = new System.IO.FileInfo(filename);

            string path = file.DirectoryName;
            if (System.IO.Directory.Exists(path))
                return enumCheckFileResult.ecfrPathIsFound;
            else
                return enumCheckFileResult.ecfrPathNotFound;
        }

    }
    //-----------------------
    /// <summary>
    /// Класс файла источник, для него важно чтобы при проверки был в наличии файл
    /// </summary>
    public class clsFileSource : clsFileBase
    {
        public clsFileSource()
            : base()
        {

        }

        public clsFileSource(string filename)
            : base(filename)
        {
            //FileName = filename;
        }
        /// <summary>
        /// Переопределена провека файла
        /// </summary>
        /// <returns></returns>
        public override enumCheckFileResult CheckFile()
        {
            enumCheckFileResult resultcheck = base.CheckFile();

            switch (resultcheck )
            {
                case enumCheckFileResult.ecfrUnknown:
                    CheckMsg = "Случилось что то непредвиденое";
                    break;
                case enumCheckFileResult.ecfrFileNotFound:
                    CheckMsg = "Исходный файл не найден, проверьте правильность указаного пути и имени файла";
                    break;
                case enumCheckFileResult.ecfrMissingFileName:
                    CheckMsg = "Некорректно задано имя файла";
                    break;
            } 

            return resultcheck;
        }
        
        //переопределено открытие файла, файл есть мы его открываем
        public override bool FileOpen()
        {
            //return base.LoadStream();
            if (CheckFile() == enumCheckFileResult.ecfrFileIsFound)
            {
                fStream = new FileStream(FileName, FileMode.OpenOrCreate);
                return true;
            }
            else
                return false;
        }

    }


    //-----------------------
    /// <summary>
    /// Класс файла назначения
    /// </summary>
    public class clsFileDestination : clsFileBase
    {
        public int NumLastProceded = -1;
        //public StreamWriter sw;
        public bool ReWrite = false;

        public clsFileDestination()
            : base()
        {

        }

        public clsFileDestination(string filename)
            : base(filename)
        {

        }
        /// <summary>
        /// Переопределена проверка файла, если мы нашли файл, нужно спросить, можно его перезаписать?
        /// если не нашли, проверяем директорию назначения, а есть ли куда записывать файл
        /// </summary>
        /// <returns></returns>
        public override enumCheckFileResult CheckFile()
        {
            enumCheckFileResult resultcheck = base.CheckFile();

            if (resultcheck == enumCheckFileResult.ecfrMissingFileName)
            { }
            else if (resultcheck == enumCheckFileResult.ecfrFileIsFound)
                CheckMsg = "Файл назначения уже есть, перезаписать его?";
            else
            {
                resultcheck = CheckDirectory(FileName);
                if (resultcheck == enumCheckFileResult.ecfrPathNotFound)
                    CheckMsg = "Путь назначения не найден";
                else
                { }

            }

            return resultcheck;
        }

        /// <summary>
        /// В теории должна вернуть расчетный размер файла :)
        /// </summary>
        public long CalcFileSize
        {
            get
            {
                return FileSize;
            }
            set
            {
                mFileSize = value;
            }
        }
        /// <summary>
        /// Переопределенная процедура, для создания или перезаписи файла назначения
        /// </summary>
        /// <returns></returns>
        public override bool FileOpen()
        {

            //if (ResultCheck)
            //{
            fStream = File.Create(FileName);
            //sw = new StreamWriter(fStream);
            return true;
            //}
            //else
            //    return false;
        }

        public void AddStreamToFile(MemoryStream srcstream)
        {
            //StreamWriter sw = new StreamWriter(fStream);
            //sw.Write(
            if (fStream != null)
            {
                if (srcstream != null)
                {
                    //srcstream.CopyTo(fStream);
                    fStream.Write(srcstream.GetBuffer(), 0, (int)srcstream.Length);
                }
            }
        }
               

    }
}
