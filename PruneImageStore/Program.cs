using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ImageDiff;

namespace PruneImageStore
{
    class Program
    {
        static int Main(string[] args)
        {
            double threshold = 10;
            int verbose = 0;
            string folderPath = null;
            bool continuous = false;
            int continuousWait = 10000;

            for (int arg = 0; arg < args.Length; ++arg)
            {
                switch (args[arg])
                {
                    case "-t":
                        ++arg;
                        if (arg > args.Length)
                        {
                            Usage();
                            return 1;
                        }

                        if (!double.TryParse(args[arg], out threshold))
                        {
                            Usage();
                            return 1;
                        }
                        break;

                    case "-v":
                        verbose = 1;
                        break;

                    case "-V":
                        verbose = 2;
                        break;

                    case "-c":
                        continuous = true;
                        break;

                    case "-w":
                        ++arg;
                        if (arg > args.Length)
                        {
                            Usage();
                            return 1;
                        }

                        if (!int.TryParse(args[arg], out continuousWait))
                        {
                            Usage();
                            return 1;
                        }
                        break;

                    default:
                        if (folderPath != null)
                        {
                            Usage();
                            return 1;
                        }
                        folderPath = args[arg];
                        break;
                }
            }
            if (folderPath == null)
            {
                Usage();
                return 1;
            }

            string lastFilePath = null;
            string fileToDelete = null;

            while (continuous)
            {
                foreach (string filePath in Directory.EnumerateFiles(folderPath))
                {
                    if (lastFilePath == null)
                    {
                        lastFilePath = filePath;
                    }
                    else if (string.Compare(lastFilePath, filePath) < 0)
                    {
                        if (verbose > 1)
                        {
                            Console.WriteLine("Compare " + filePath + " with " + lastFilePath);
                        }

                        Comparison c = new Comparison(lastFilePath, filePath);
                        if (fileToDelete != null)
                        {
                            if (verbose > 1)
                            {
                                Console.WriteLine("Delete " + fileToDelete);
                            }
                            File.Delete(fileToDelete);
                            fileToDelete = null;
                        }

                        if (verbose > 1)
                        {
                            Console.WriteLine("\tAAD: " + c.AverageAbsoluteDifference + "\tASD: " + c.AverageSquareDifference);
                        }

                        if (c.AverageSquareDifference < threshold)
                        {
                            if (verbose > 0)
                            {
                                Console.WriteLine("Old is same as new (ASD = " + c.AverageSquareDifference + "), delete new: " + Path.GetFileName(filePath));
                            }
                            fileToDelete = filePath;
                        }

                        lastFilePath = filePath;
                    }
                    else
                    {
                    }
                }

                if (continuous)
                {
                    if (verbose > 0)
                    {
                        Console.WriteLine("Sleeping " + continuousWait + " milliseconds.");
                    }
                    System.Threading.Thread.Sleep(continuousWait);
                }
            }

            if (fileToDelete != null)
            {
                if (verbose > 1)
                {
                    Console.WriteLine("Delete " + fileToDelete);
                }
                File.Delete(fileToDelete);
                fileToDelete = null;
            }

            return 0;
        }

        private static void Usage()
        {
            Console.WriteLine("PruneImageStore <folder-path> [-t <threshold>] [-v] [-V] [-c -w <milliseconds>]");
        }
    }
}
