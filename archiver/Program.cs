using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace archiver
{


    /*
     * archiver.exe compress -itest.txt -otest.gz -b2097152
     * archiver.exe compress -itest.txt -otest.gz
     * archiver.exe decompress -itest.gz -otest_.txt
     */


    class Program
    {
        static void Main(string[] args)
        {
            clsArchiver Arc = new clsArchiver();
            bool BadParam = false;

            if (args.Length > 0)
            {
                for (int i = 0; i <= args.Length - 1; i++)
                {
                 //   Console.WriteLine(args[i].ToString().Trim());

                    Regex rStr;
                    MatchCollection coll;

                    rStr = new Regex("^(?<value>[cC][oO][mM][pP][rR][eE][sS][sS])");
                    coll = rStr.Matches((args[i].ToString().Trim()));
                    if (coll.Count > 0)
                        Arc.OperationType = enumOperationType.eotCompress;
                    else
                    {
                        rStr = new Regex("^(?<value>[dD][eE][cC][oO][mM][pP][rR][eE][sS][sS])");
                        coll = rStr.Matches((args[i].ToString().Trim()));
                        if (coll.Count > 0)
                            Arc.OperationType = enumOperationType.eotDecompress;
                        else
                        {
                            rStr = new Regex("^(?<param>[/\\:-][iI])(?<value>[\\a-zA-Z0-9.,;#$@/:-]*)");
                            coll = rStr.Matches((args[i].ToString().Trim()));
                            if (coll.Count > 0)
                                Arc.FileSourceName = coll[0].Groups["value"].ToString();
                            else
                            {
                                rStr = new Regex("^(?<param>[/\\:-][oO])(?<value>[\\a-zA-Z0-9.,;#$@/:-]*)");
                                coll = rStr.Matches((args[i].ToString().Trim()));
                                if (coll.Count > 0)
                                    Arc.FileDestinationName = coll[0].Groups["value"].ToString();
                                else
                                {
                                    rStr = new Regex("^(?<param>[/\\:-][bB])(?<value>[0-9]*)");
                                    coll = rStr.Matches((args[i].ToString().Trim()));

                                    if (coll.Count > 0)
                                        int.TryParse(coll[0].Groups["value"].ToString(), out Arc.iBlockSize);

                                }
                            }

                        }
                    }
                }

                  if (Arc.CheckParameters() == false)
                      BadParam = true;

            }
            else
                BadParam = true;

            if (BadParam == true)
            {
                Arc.ShowErrs();
                GetHelp();
            }
            else
                if (Arc.ProcessFile())
                    Console.WriteLine("Complete");
                else
                    Arc.ShowErrs();
                    
            Console.WriteLine("");
            Console.WriteLine("Для завершения программы нажмите любую клавишу");
            Console.Read();
                

        }

        static void GetHelp()
        {
            Console.WriteLine("Параметры запуска программы:");
            Console.WriteLine(string.Format(@"archiver.exe compress/decompress [-i][имя исходного файла] [-o][имя результирующего файла] [-b]{0}", clsArchiver.DefaultBlockSize));
            Console.WriteLine(@"compress / decompress - параметр задающий действие, compress - архивирование файла, decompress - разорхивирование");
            Console.WriteLine(@"[-i][имя исходного файла] - файл, над которым необходимо произвести операцию");
            Console.WriteLine(@"[-o][имя результирующего файла] - файл, в который необходимо записать результат");
            Console.WriteLine(string.Format(@"[-b][размер блока] - размер блока файла в байтах, каждый блок архивируется отдельным потоком. По умолчанию - {0} байт.", clsArchiver.DefaultBlockSize));



        }
    }
}
