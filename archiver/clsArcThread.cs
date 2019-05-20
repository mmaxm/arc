using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace archiver
{
    /// <summary>
    /// Класс хранит поток для обработки, входящие и исходящие данные
    /// </summary>
    public class clsArcThread
    {
        //public int ThreadNumber;
        /// <summary>
        /// Не запакованые данные
        /// </summary>
        public byte[] DataArray;
        /// <summary>
        /// Запакованые данные
        /// </summary>
        public byte[] CompressedDataArray;
        /// <summary>
        /// Поток для обработки данных
        /// </summary>
        public Thread wrkThread;

    }
}
