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

            string comparisonFilePath = null;
            bool firstRun = true;

            while (continuous || firstRun)
            {
                firstRun = false;
                foreach (string filePath in Directory.EnumerateFiles(folderPath))
                {
                    if (comparisonFilePath == null)
                    {
                        comparisonFilePath = filePath;
                    }
                    else if (string.Compare(comparisonFilePath, filePath) < 0)
                    {
                        if (verbose > 1)
                        {
                            Console.WriteLine("Compare " + filePath + " with " + comparisonFilePath);
                        }

                        Comparison c = new Comparison(comparisonFilePath, filePath);
                        try
                        {
                            c.CompareImages(false);

                            if (verbose > 1)
                            {
                                Console.WriteLine("\tAAD: " + c.AverageAbsoluteDifferenceText + "\tASD: " + c.AverageSquareDifferenceText);
                            }

                            if (c.AverageSquareDifference < threshold)
                            {
                                if (verbose > 0)
                                {
                                    Console.WriteLine("Old is same as new (ASD = " + c.AverageSquareDifferenceText + "), delete new: " + Path.GetFileName(filePath));
                                }
                                if (verbose > 1)
                                {
                                    Console.WriteLine("Delete " + filePath);
                                }
                                File.Delete(filePath);
                            }
                            else
                            {
                                comparisonFilePath = filePath;
                            }
                        }
                        // I've seen two kinds of errors, but we should survive any.
                        // I've seen IOExceptions because some other process had the file open, and
                        // I've seen a NotSupportedException due to a failure to create the decoder because of file corruption issues.
                        catch (IOException e)
                        {
                            Console.Error.WriteLine("IOException: " + e.Message);
                            Console.Error.WriteLine("Files");
                            Console.Error.WriteLine("\tLeft:\t" + comparisonFilePath);
                            Console.Error.WriteLine("\tRight:\t" + filePath);
                            comparisonFilePath = filePath;
                        }
                        catch (NotSupportedException e)
                        {
                            Console.Error.WriteLine("NotSupportedException: " + e.Message);
                            Console.Error.WriteLine("Files");
                            Console.Error.WriteLine("\tLeft:\t" + comparisonFilePath);
                            Console.Error.WriteLine("\tRight:\t" + filePath);
                            comparisonFilePath = filePath;
                        }
                    }
                    else
                    {
                        // This is the case where continuous is true, and
                        // we've already seen the file in the prior iteration.
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

            return 0;
        }

        private static void Usage()
        {
            Console.WriteLine("PruneImageStore <folder-path> [-t <threshold>] [-v] [-V] [-c -w <milliseconds>]");
        }
    }
}
