using System;
using System.IO;

namespace files_to_letterboxd_csv
{
    class Program
    {
        static void Main(string[] args)
        {
            string directoryPath = @"E:/Cinema/E-ManualRename";
            string fileName = Path.GetFileNameWithoutExtension(directoryPath);

            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            FileInfo[] files = directory.GetFiles();

            string csv = "Title,Year," + Environment.NewLine;

            Console.WriteLine(directory.FullName);
            foreach (FileInfo file in files)
            {
                Console.WriteLine(Path.GetFileNameWithoutExtension(file.FullName));
                string[] titleYearSplit = Path.GetFileNameWithoutExtension(file.FullName).Split('(');
                string title = titleYearSplit[0].TrimEnd(' ').Replace(",", "");
                string year = titleYearSplit[1].Replace("(", "").Replace(")", "");
                Console.WriteLine("After the split");
                csv += title + "," + year + "," + Environment.NewLine;
            }
            File.WriteAllText(directoryPath + @"\1-" + fileName + "-letterboxd-list.csv", csv);
        }
    }
}
