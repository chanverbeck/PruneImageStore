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
            if (args.Length != 1)
            {
                Console.WriteLine("Need to specify a folder name");
                return 1;
            }

            string folderPath = args[0];
            string lastFilePath = null;
            string fileToDelete = null;

            foreach (string filePath in Directory.EnumerateFiles(folderPath))
            {
                if (lastFilePath != null)
                {
                    Console.WriteLine("Compare " + filePath + " with " + lastFilePath);

                    Comparison c = new Comparison(lastFilePath, filePath);
                    if (fileToDelete != null)
                    {
                        Console.WriteLine("Actually delete " + fileToDelete);
                        fileToDelete = null;
                    }

                    Console.WriteLine("\tAAD: " + c.AverageAbsoluteDifference + "\tASD: " + c.AverageSquareDifference);

                    if (c.AverageSquareDifference < 10.0)
                    {
                        Console.WriteLine("Old is same as new (ASD = " + c.AverageSquareDifference + "), delete new: " + Path.GetFileName(filePath));
                        fileToDelete = filePath;
                    }
                }

                lastFilePath = filePath;
            }

            return 0;
        }
    }
}
