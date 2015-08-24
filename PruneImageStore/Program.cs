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
            bool dryRun = false;
            string logFileName = null;

            for (int arg = 0; arg < args.Length; ++arg)
            {
                switch (args[arg])
                {
                    case "-t":
                        ++arg;
                        if (arg >= args.Length)
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
                        if (arg >= args.Length)
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

                    case "-l":
                        ++arg;
                        if (arg >= args.Length)
                        {
                            Usage();
                            return 1;
                        }

                        logFileName = args[arg];
                        break;

                    case "-d":
                        dryRun = true;
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

            TextWriter logWriter = Console.Out;
            if (logFileName != null)
            {
                logWriter = new StreamWriter(logFileName, true);
            }

            string comparisonFilePath = null;
            bool firstRun = true;

            while (continuous || firstRun)
            {
                int deletedFileCount = 0;
                firstRun = false;
                if (verbose > 0)
                {
                    logWriter.WriteLine("==========================");
                    logWriter.WriteLine("Prune " + folderPath + " at " + DateTime.Now + " (" + GetTimeZoneName() + ")");
                    logWriter.WriteLine("==========================");
                    logWriter.Flush();
                }

                foreach (string filePath in Directory.EnumerateFiles(folderPath, "*.jpg"))
                {
                    if (comparisonFilePath == null)
                    {
                        comparisonFilePath = filePath;
                    }
                    else if (string.Compare(comparisonFilePath, filePath) < 0)
                    {
                        if (verbose > 1)
                        {
                            logWriter.WriteLine("Compare " + filePath + " with " + comparisonFilePath);
                        }

                        Comparison c = new Comparison(comparisonFilePath, filePath);
                        try
                        {
                            c.CompareImages(false);

                            if (verbose > 1)
                            {
                                logWriter.WriteLine("\tAAD: " + c.AverageAbsoluteDifferenceText + "\tASD: " + c.AverageSquareDifferenceText);
                            }

                            if (c.AverageSquareDifference < threshold)
                            {
                                ++deletedFileCount;
                                if (verbose > 0)
                                {
                                    logWriter.WriteLine("Old is same as new (ASD = " + c.AverageSquareDifferenceText + "), delete new: " + Path.GetFileName(filePath));
                                }
                                if (!dryRun)
                                {
                                    if (verbose > 1)
                                    {
                                        logWriter.WriteLine("Delete " + filePath);
                                    }
                                    File.Delete(filePath);
                                }
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
                            if (logWriter != Console.Out)
                            {
                                logWriter.WriteLine("IOException: " + e.Message);
                                logWriter.WriteLine("Files");
                                logWriter.WriteLine("\tLeft:\t" + comparisonFilePath);
                                logWriter.WriteLine("\tRight:\t" + filePath);
                            }
                            comparisonFilePath = filePath;
                        }
                        catch (NotSupportedException e)
                        {
                            Console.Error.WriteLine("NotSupportedException: " + e.Message);
                            Console.Error.WriteLine("Files");
                            Console.Error.WriteLine("\tLeft:\t" + comparisonFilePath);
                            Console.Error.WriteLine("\tRight:\t" + filePath);
                            if (logWriter != Console.Out)
                            {
                                logWriter.WriteLine("NotSupportedException: " + e.Message);
                                logWriter.WriteLine("Files");
                                logWriter.WriteLine("\tLeft:\t" + comparisonFilePath);
                                logWriter.WriteLine("\tRight:\t" + filePath);
                            }
                            comparisonFilePath = filePath;
                        }
                    }
                    else
                    {
                        // This is the case where continuous is true, and
                        // we've already seen the file in the prior iteration.
                    }

                    logWriter.Flush();
                }

                logWriter.WriteLine("Deleted " + deletedFileCount + " files.");
                if (continuous)
                {
                    if (verbose > 0)
                    {
                        logWriter.WriteLine("Sleeping " + continuousWait + " milliseconds.");
                    }
                    System.Threading.Thread.Sleep(continuousWait);
                }
            }

            if (logWriter != Console.Out)
            {
                logWriter.Close();
            }

            return 0;
        }

        private static void Usage()
        {
            Console.Error.WriteLine("PruneImageStore <folder-path> [-t <threshold>] [-v] [-V] [-c -w <ms>] [-d] [-l <log-file>]");
            Console.Error.WriteLine("<folder-path>:\tThe folder to prune.");
            Console.Error.WriteLine("-t <threshold>:\tWhat Average Square Difference is considered \"same.\"");
            Console.Error.WriteLine("-v and -V:\tVerbose.");
            Console.Error.WriteLine("-c:\t\tContinuously. Defaults to wait 10000 ms between cycle.");
            Console.Error.WriteLine("-w <ms>:\tThe time to wait between continuous executions in milliseconds.");
            Console.Error.WriteLine("-d:\t\tDry run - delete no files.");
            Console.Error.WriteLine("-l <log-file>:\tLog to a file instead of stdout.");
        }

        private static string GetTimeZoneName()
        {
            if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(DateTime.Now))
            {
                return TimeZone.CurrentTimeZone.DaylightName;
            }
            else
            {
                return TimeZone.CurrentTimeZone.StandardName;
            }
        }
    }
}
