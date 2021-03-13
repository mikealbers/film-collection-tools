using System;

namespace tmdb_file_rename
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Please provide a TMDB api key");
            }
            else 
            {
                Console.WriteLine(args[1]);
            }

        }
    }
}
