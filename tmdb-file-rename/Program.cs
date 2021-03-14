using System;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace tmdb_file_rename
{
    class Program
    {
        public const string TMDB_SEARCH_URL = "https://api.themoviedb.org/3/search/movie?api_key=";

        static void Main(string[] args)
        {
            Console.WriteLine("----------------------------------------\n" +
                "|        TMDB File Renaming Tool       |\n" +
                "|           Mike Albers 2021           |\n" +
                "|                                      |\n" +
                "| This tool will search TMDB for movie |\n" +
                "| titles based on a supplied directory |\n" +
                "| and rename the files from the results|\n" +
                "|                                      |\n" +
                "|                                      |\n" +
                "|    Requires a TMDB api key to use    |\n" +
                "|                                      |\n" +
                "|                                      |\n" +
                "----------------------------------------\n");


            Console.WriteLine("Enter a TMDB api key:");
            string apiKey = Console.ReadLine();
            Console.WriteLine("Enter a directory path:");
            string directoryPath = Console.ReadLine();

            //TODO: Remove. Temporarily for testing purposes.
            //apiKey = args[1];
            //directoryPath = @"E:\Cinema";

            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            FileInfo[] files = directory.GetFiles();

            Tuple<List<string>, List<string>> selectedAndSkippedNames = GetSelectedAndSkippedNamesFromUser(files, apiKey);
            List<string> selectedNames = new List<string>(selectedAndSkippedNames.Item1);
            List<string> skippedFiles = new List<string>(selectedAndSkippedNames.Item2);

            Console.WriteLine("----------------------------------------\n" +
                "----------------------------------------\n" +
                "The files in this directory will be renamed as followed:");
            foreach (string selectedName in selectedNames)
            {
                Console.WriteLine(selectedName);
            }
            Console.WriteLine("----------------------------------------\n" +
                "----------------------------------------\n" +
                "Confirm rename (y/n)");

            ConsoleKey response;

            do
            {
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine("");
                }

            }
            while (response != ConsoleKey.Y && response != ConsoleKey.N);

            if (response == ConsoleKey.Y)
            {
                int fileCount = 0;
                foreach (FileInfo file in files)
                {
                    if (Path.GetFileNameWithoutExtension(file.FullName) != selectedNames[fileCount])
                    {
                        try
                        {
                            File.Move(file.FullName, file.FullName.Replace(Path.GetFileNameWithoutExtension(file.FullName), selectedNames[fileCount]));
                        }
                        catch
                        {
                            
                        }
                    }
                    fileCount++;
                }

                TextWriter tw = new StreamWriter(directoryPath + @"\TMDB-skipped-files.txt");

                foreach (String s in skippedFiles)
                    tw.WriteLine(s);

                tw.Close();
                return;
            }
            else
            {
                Console.WriteLine("Exiting program");
                return; 
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

        public static string FormatFileNameForQuery(string fileName)
        {
            var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            //TODO: Refactor this. Should be reworked so that there are not as many TrimEnd functions / generally cleaner
            string formatted = Path.GetFileNameWithoutExtension(fileName);
            formatted = formatted.Replace(" ", "+").Replace(".", "+").Replace("_", "+").Replace("1080p", "").Replace("720p", "");
            formatted = formatted.TrimEnd('+');
            formatted = formatted.TrimEnd(digits);
            formatted = formatted.TrimEnd('+');

            //Commented out to skip already formatted files
            //int index = formatted.LastIndexOf("(");
            //if (index > 0)
            //    formatted = formatted.Substring(0, index);

            return formatted;
        }

        public static string FormatNewNameFromMovie(Movie movie)
        {
            string newName = movie.Title;
            if (!String.IsNullOrWhiteSpace(movie.Release_Date)) newName += " (" + movie.Release_Date.Substring(0, movie.Release_Date.IndexOf('-')) + ")";
            return newName;
        }

        public static Tuple<List<string>,List<string>> GetSelectedAndSkippedNamesFromUser(FileInfo[] files, string apiKey)
        {
            int fileCounter = 0;
            List<string> selectedNames = new List<string>();
            List<string> skippedFiles = new List<string>();

            foreach (FileInfo file in files)
            {
                fileCounter++;
                Console.WriteLine("[{0} of {1}]", fileCounter, files.Length);

                //Call TMDB api for list of movies matching filename
                //The files names need to be formated beforehand for TMDB's search
                string formattedFileName = FormatFileNameForQuery(file.FullName);
                string details = GetRestMethod(TMDB_SEARCH_URL + apiKey + "&query=" + formattedFileName);
                Response detailsDeserialized = JsonConvert.DeserializeObject<Response>(details);

                if (detailsDeserialized.Total_Pages > 1)
                {
                    for (int i = 2; i <= detailsDeserialized.Total_Pages; i++)
                    {
                        Response pagesToAdd = JsonConvert.DeserializeObject<Response>(GetRestMethod(TMDB_SEARCH_URL + apiKey + "&query=" + formattedFileName + "&page=" + i));
                        detailsDeserialized.Results.AddRange(pagesToAdd.Results);
                    }
                }

                List<string> movieNames = new List<string>();
                foreach (Movie movie in detailsDeserialized.Results)
                {
                    var newName = FormatNewNameFromMovie(movie);
                    movieNames.Add(newName);
                }

                if (movieNames.Count == 0)
                {
                    skippedFiles.Add(Path.GetFileNameWithoutExtension(file.FullName));
                    selectedNames.Add(Path.GetFileNameWithoutExtension(file.FullName));
                    Console.WriteLine("Zero results for: {0} \n" +
                        "Skipping rename\n" +
                        "---------------------------------------", file.FullName);
                }
                else if (movieNames.Count == 1)
                {
                    selectedNames.Add(movieNames[0]);
                    Console.WriteLine("One result for: {0} \n" +
                        "{1}\n" +
                        "---------------------------------------", file.FullName, movieNames[0]);
                }
                else
                {
                    Console.WriteLine("Mutiple results for: {0}\n" +
                        "Please choose from one of the following:\n" +
                        "----------------------------------------", file.FullName);
                    Tuple<string,bool> selection = NameSelectionMenu(movieNames, Path.GetFileNameWithoutExtension(file.FullName));

                    if (selection.Item2) skippedFiles.Add(selection.Item1);
                    selectedNames.Add(selection.Item1);
                }
            }
            return Tuple.Create(selectedNames,skippedFiles);
        }

        public static Tuple<string, bool> NameSelectionMenu(List<string> movieNameOptions, string originalName)
        {
            int selectionCounter = 0;
            int selectionInput;
            bool skipRename = false;
            Dictionary<int, string> selectionLookup = new Dictionary<int, string>();

            foreach(string movieName in movieNameOptions)
            {
                selectionCounter++;
                selectionLookup.Add(selectionCounter, movieName);
                Console.WriteLine("{0}.) {1}", selectionCounter, movieName);
            }

            Console.WriteLine("{0}.) SKIP and keep original filename.", selectionCounter + 1);
            selectionLookup.Add(selectionCounter + 1, originalName);

            while (!int.TryParse(Console.ReadLine(), out selectionInput) || selectionInput > selectionCounter + 1)
            {
                Console.WriteLine("Invalid selection \n" +
                    "Enter a value between 1 and {0}", selectionCounter + 1);
            }

            if (selectionInput == selectionCounter + 1)
            {
                Console.WriteLine("Skipping rename\n");
                skipRename = true;
            }   
            else
            {
                Console.WriteLine("Rename selection:\n{0}", selectionLookup[selectionInput]);
            }


            return Tuple.Create(selectionLookup[selectionInput], skipRename);
        }

        public class Response
        {
            public int TotalResults { get; set; }
            public int Total_Pages { get; set; }
            public List<Movie> Results { get; set; }
        }

        public class Movie
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Original_Title { get; set; }
            public string Release_Date { get; set; } 
        }
    }
}
