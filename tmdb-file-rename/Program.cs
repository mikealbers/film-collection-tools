using System;
using System.Net;
using System.IO;
using System.Text;

namespace tmdb_file_rename
{
    class Program
    {
        public const string TMDB_SEARCH_URL = "https://api.themoviedb.org/3/search/movie";

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Please provide a TMDB api key");
            }
            else 
            {
                Console.WriteLine(args[1]);
                //TODO: replace arguments with prompts
                //Console.WriteLine("Enter a TMDB api key");
                //string apiKey = Console.ReadLine();
                //Console.WriteLine("Enter a directory path");
                //string directory = Console.Readline();
                //C:\Users\malbe\Downloads\films
                //E:\Cinema

                //var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

                //DirectoryInfo directory = new DirectoryInfo(@"E:\Cinema");
                //FileInfo[] infos = directory.GetFiles();
                //foreach (FileInfo f in infos)
                //{
                //    string formatted = Path.GetFileNameWithoutExtension(f.FullName);
                //    formatted = formatted.Replace(" ", "+").Replace(".", "+").Replace("_", "+").Replace("1080p", "").Replace("720p", "");
                //    formatted = formatted.TrimEnd('+');
                //    formatted = formatted.TrimEnd(digits);
                //    formatted = formatted.TrimEnd('+');

                //    Console.WriteLine(formatted);
                //    Console.WriteLine(f.FullName);
                //    Console.WriteLine("----------------------------------------");
                //}

                //string queryBase = TMDB_SEARCH_URL + "?api_key=" + args[1] + "&query=";
                //string filmQuery = queryBase + "Tai+Club";

                //string details = GetRestMethod(filmQuery);

                //Console.WriteLine(details);
            }

        }

        public static string GetRestMethod(string url)
        {
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(url);
            webrequest.Method = "GET";
            webrequest.ContentType = "application/x-www-form-urlencoded";
            HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();
            Encoding enc = Encoding.GetEncoding("utf-8");
            StreamReader responseStream = new StreamReader(webresponse.GetResponseStream(), enc);
            string result = responseStream.ReadToEnd();
            webresponse.Close();
            return result;
        }
    }
}
