using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CatiaToCGR
{
    class Program
    {
        const string confFile = "CatiaToCgr.conf";
        const string catFilesToProcess = "CATProductsToProcess";
        //static string rootFld = @"\\stormngodr\Odjeli\TU-SET\KON\Projekti";
        //static string listFld = @"D:\temp\CATIA\ListaDijelova";
        //static string cgrFld = @"D:\temp\CATIA\Arhiva za vizualno pretraživanje 3D modela";
        static string rootFld;
        static string listFld;
        static string cgrFld;
        static string cgrFldx;

        static String[] fileExt = new string[]
        {
                "CATProduct",
                "CATPart"
        };
        static CatiaApp catia = new CatiaApp();
        static decimal Price => 3000;
        static int myID { get; } = 1000;


        static void Main(string[] args)
        {
            Console.WriteLine("Price := " + Price);
            Console.WriteLine("myID := " + myID);
            Console.ReadLine();
            return;

            DateTime start_time, stop_time;
            TimeSpan elapsed_time;
            int level = 3;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");

            if (!ReadConfiguration())
                return;

            List<string> files =
                GetFiles(level, args.Count() == 0 ? String.Empty : args[0]);
            if (files == null)
                return;

            if (!catia.GetOrCreateInstance())
                return;

            start_time = DateTime.Now;
            ConvertToCGR(files);

            stop_time = DateTime.Now;
            elapsed_time = stop_time.Subtract(start_time);
            Console.WriteLine("Vrijeme obrade : " +
                elapsed_time.TotalSeconds.ToString("0.00") + " sekunde.\n");
            try
            {
                catia.myCATIA.Quit();
            }
            catch (Exception)
            {
                if (catia.myCATIA == null)
                    Console.WriteLine("CATIA has crashed !");
                else
                    Console.WriteLine("Cannot quit CATIA  !");
            }
            Console.WriteLine("Press any key to continue...");
            //Console.ReadLine();

        }


        static bool ReadConfiguration()
        {
            var confPath = Directory.GetCurrentDirectory();
            if (!File.Exists(confPath + "\\" + confFile))
            {
                confPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!File.Exists(confPath + "\\" + confFile))
                {
                    Console.WriteLine("No configuration file --> " + 
                        confPath + "\\" + confFile + " !");
                    Console.ReadLine();
                    return false;
                }
            }

            var conf = File.ReadAllLines(confPath + "\\" + confFile)
                .Where(p => p.Split(new char[] { '=' }).Count() == 2)
                .Select(s => s.Split(new char[] { '=' }))
                .Select(s => new Tuple<string, string>
                (
                    s[0].ToString().Trim(),
                    s[1].ToString().Trim()
                ));

            foreach (var row in conf)
            {
                switch (row.Item1)
                {
                    case "RootFolder":
                        rootFld = row.Item2;
                        break;
                    case "InOutFolder":
                        listFld = row.Item2;
                        break;
                    case "CatiaFolder":
                        cgrFld = row.Item2;
                        break;
                    default:
                        Console.WriteLine("No such configuration parameter := " + row.Item1);
                        break;
                }
            }
            if (rootFld == null)
            {
                Console.WriteLine("Parameter RootFolder not set!");
                return false;
            }
            else if (listFld == null)
            {
                Console.WriteLine("Parameter InOutFolder not set!");
                return false;
            }
            else if (cgrFld == null)
            {
                Console.WriteLine("Parameter CatiaFolder not set!");
                return false;
            }
            else
                return true;
        }

        static List<string> GetFiles(int level, string path)
        {
            DateTime start_time, stop_time;
            TimeSpan elapsed_time;

            List<string> files = new List<string>();
            if (path == String.Empty)
            {
                string answer = "N";
                Console.WriteLine("Do you really want to process all the files in \n'" +
                    rootFld + "' folder? :=(D/N) ");
                answer = Console.ReadLine().ToUpper();
                if (answer == "D")
                {
                    Console.WriteLine("Enter max number of lines per file := ");
                    int nLines = 0;
                    Int32.TryParse(Console.ReadLine(), out nLines);

                    Console.WriteLine("Scanning CATIA files in level " + level.ToString() + 
                        " of directory :=\n\t '" + rootFld + "' ...");
                    start_time = DateTime.Now;
                    FolderLevelFiles(rootFld, level, fileExt[0], files);
                    stop_time = DateTime.Now;
                    elapsed_time = stop_time.Subtract(start_time);
                    Console.WriteLine("Scan time : " +
                        elapsed_time.TotalSeconds.ToString("0.00") + " secs.\n");

                    Console.WriteLine("Total number of lines := " + files.Count + ".");
                    files = files.Distinct().ToList();
                    Console.WriteLine("Total number of distinct lines := " + files.Count + ".");
                    Console.WriteLine("Writing files to '" + listFld + "\\" +
                        catFilesToProcess + ".txt' ...");
                    StreamWriter sw = new StreamWriter(listFld + "\\" + catFilesToProcess + ".txt");
                    if (nLines == 0) nLines = files.Count;
                    int inx = 0;
                    for (int i = 0; i < files.Count; i++)
                    {
                        sw.WriteLine(files[i]);
                        if (i+1 != files.Count && (i+1) % nLines == 0)
                        {
                            sw.Close();
                            inx++;
                            sw = new StreamWriter(listFld + "\\" + catFilesToProcess +
                                inx.ToString("D3") + ".txt");
                        }
                    }
                    sw.Close();

                    Console.WriteLine("See output file(s) --> '" + 
                        listFld + "\\" + catFilesToProcess + "N.txt'.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadLine();
                    return null;
                }
                else
                {
                    Console.WriteLine("There are no files to process.\n" +
                        "Press any key to exit...");
                    Console.ReadLine();
                    return null;
                }
            }
            else
            {
                try
                {
                    Console.WriteLine("Reading files from '" + listFld + "\\" +
                        path + "'...");
                    files = System.IO.File.ReadAllLines(listFld + "\\" +
                        path).ToList();
                    Console.WriteLine(files.Count + " files to be processed...");
                    return files;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadLine();
                    return null;
                }
            }

            //var fls = System.IO.File.ReadAllLines("ProjectCATProducts.txt");
            //var bslash = fls.Where(p => p.Count(c => c == '\\') == 9).ToList();

            //Console.WriteLine("Reference number := " + bslash.Count);
            //Console.WriteLine("Found number := " + files.Count);
            //Console.ReadLine();
        }


        static void ConvertToCGR(List<string> files)
        {
            //string rootFld = @"D:\temp\CATIA\Projekti";

            if (File.Exists(listFld + @"\CATProductList.txt"))
                File.Delete(listFld + @"\CATProductList.txt");
            StreamWriter sw = new StreamWriter(listFld + @"\CATProductList.txt", true);
            //foreach (var file in files)
            for (int i = 0; i < files.Count; i++)
            {
                string prodDocName = catia.ExportToCgr(files[i], cgrFld);
                if (catia.myCATIA == null)
                {
                    Console.WriteLine("CATIA is Down ! --> Product Name := " + files[i]);
                    if (!catia.GetOrCreateInstance())
                        return;
                }
                Console.WriteLine("Nbr of docs:= " + catia.myCATIA.Documents.Count +
                    " --> " + files[i]);
                if (prodDocName != null)
                {
                    catia.SaveCgrToProject(prodDocName);
                    sw.WriteLine(files[i]);
                    sw.Flush();
                }
            }
            sw.Close();
        }


        static void FolderLevelFiles(string folder, int level, string fileType,
            List<string> files)
        {
            try
            {
                var subDirs = Directory.GetDirectories(folder);

                if (level > 1)
                {
                    foreach (var dir in subDirs)
                        FolderLevelFiles(dir, level - 1, fileType, files);
                }
                if (level == 1)
                {
                    foreach (var file in Directory.GetFiles(folder))
                    {
                        if (Path.GetExtension(file) == "." + fileType)
                            files.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
