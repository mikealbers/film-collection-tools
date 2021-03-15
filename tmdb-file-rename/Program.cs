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
        public const string TITLE = "----------------------------------------\n" +
                "|        TMDB File Renaming Tool       |\n" +
                "|           Mike Albers 2021           |\n" +
                "|                                      |\n" +
                "| This tool will search TMDB for movie |\n" +
                "| titles based on a supplied directory |\n" +
                "| and rename the files from the        |\n" +
                "| formatted api response               |\n" +
                "|                                      |\n" +
                "|    Requires a TMDB api key to use    |\n" +
                "|                                      |\n" +
                "|                                      |\n" +
                "----------------------------------------\n";

        static void Main(string[] args)
        {
            Console.WriteLine(TITLE);

            Console.WriteLine("Enter a TMDB api key:");
            string apiKey = Console.ReadLine();
            Console.WriteLine("Enter a directory path:");
            string directoryPath = Console.ReadLine();

            //TODO: Remove. For testing purposes.
            //apiKey = args[1];
            //directoryPath = @"F:/Film";

            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            FileInfo[] files = directory.GetFiles();

            Tuple<List<string>, List<string>> selectedAndSkippedNames = GetSelectedAndSkippedNamesFromUser(files, apiKey);

            ConsoleKey userResponse = GetUserConfirmation(selectedAndSkippedNames.Item1);

            if (userResponse == ConsoleKey.Y)
            {
                RenameFiles(files, selectedAndSkippedNames.Item1);
                WriteListToTextFile(directoryPath, selectedAndSkippedNames.Item2, @"\TMDB-skipped-files.txt");
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

        public static void WriteListToTextFile(string directoryPath, List<string> stringList, string output)
        {
            TextWriter tw = new StreamWriter(directoryPath + output);
            foreach (String s in stringList)
            {
                tw.WriteLine(s);
            }
            tw.Close();
        }

        public static void RenameFiles(FileInfo[] files, List<string> newFileNames)
        {
            int fileCount = 0;
            foreach (FileInfo file in files)
            {
                if (Path.GetFileNameWithoutExtension(file.FullName) != newFileNames[fileCount])
                {
                    try
                    {
                        File.Move(file.FullName, file.FullName.Replace(Path.GetFileNameWithoutExtension(file.FullName), newFileNames[fileCount]));
                    }
                    catch(Exception e)
                    {
                        //TODO: Add some error handling. Had issues in the past with punctuation being left in the new file names.
                        Console.WriteLine(e);
                    }
                }
                fileCount++;
            }
        }

        public static ConsoleKey GetUserConfirmation(List<string> selectedNames)
        {
            // Print out all of the user selections
            // including the automatically skipped file names
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
            return response;
        }

        public static string FormatFileNameForQuery(string fileName)
        {
            var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            //TODO: Refactor this. Should be reworked so that there are not as many TrimEnd functions / generally cleaner
            string formatted = Path.GetFileNameWithoutExtension(fileName);
            formatted = formatted.Replace(" ", "+").Replace(".", "+").Replace("_", "+").Replace("1080p", "").Replace("720p", "").Replace(",","").Replace("The", "");
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

                FormattedResponse formattedResponse = GetFormattedResponse(apiKey, file.FullName);

                if (formattedResponse.MovieTitles.Count == 0)
                {
                    skippedFiles.Add(Path.GetFileNameWithoutExtension(file.FullName));
                    selectedNames.Add(Path.GetFileNameWithoutExtension(file.FullName));
                    Console.WriteLine("Zero results for: {0} \n" +
                        "Skipping rename\n" +
                        "---------------------------------------", file.FullName);
                }
                else if (formattedResponse.MovieTitles.Count == 1)
                {
                    selectedNames.Add(formattedResponse.MovieTitles[0]);
                    Console.WriteLine("One result for: {0} \n" +
                        "{1}\n" +
                        "---------------------------------------", file.FullName, formattedResponse.MovieTitles[0]);
                }
                else
                {
                    Console.WriteLine("{0} results for: {1}\n" +
                        "Please choose from one of the following:\n" +
                        "----------------------------------------", formattedResponse.Total_Results, file.FullName);
                    Tuple<string,bool> selection = NameSelectionMenu(formattedResponse);

                    if (selection.Item2) skippedFiles.Add(selection.Item1);
                    selectedNames.Add(selection.Item1);
                }
            }
            return Tuple.Create(selectedNames,skippedFiles);
        }

        public static Tuple<string, bool> NameSelectionMenu(FormattedResponse formattedResponse, int selectionCounter = 0, Dictionary<int, string> selectionLookup = null)
        {
            int selectionInput;
            int maximumSelectionNumber;
            bool skipRename = false;

            // Only add skip option on first call
            if (selectionCounter == 0 && selectionLookup == null)
            {
                selectionLookup = new Dictionary<int, string>();
                Console.WriteLine("0.) SKIP and keep original filename.");
                selectionLookup.Add(selectionCounter, formattedResponse.OriginalFileName);
            } 

            // Print out all the results for the current page
            foreach (string movieName in formattedResponse.MovieTitles)
            {
                selectionCounter++;
                selectionLookup.Add(selectionCounter, movieName);
                Console.WriteLine("{0}.) {1}", selectionCounter, movieName);
            }

            
            if (formattedResponse.Total_Pages > 1 && formattedResponse.CurrentPage != formattedResponse.Total_Pages)
            {
                // If there is more than one page and we are not on the last page, add an option to request more pages. 
                maximumSelectionNumber = selectionCounter + 1;
                Console.WriteLine("{0}.) Next page", selectionCounter + 1);
            }
            else
            {
                // Effects the input filter.
                maximumSelectionNumber = selectionCounter;
            }

            // Input filter to remove non numbers and anything outside the selection range
            while (!int.TryParse(Console.ReadLine(), out selectionInput) || selectionInput > maximumSelectionNumber)
            {
                Console.WriteLine("Invalid selection \n" +
                    "Enter a value between 0 and {0}", maximumSelectionNumber);
            }

            if (selectionInput == 0)
            {
                Console.WriteLine("Skipping rename\n");
                skipRename = true;
            }
            else if (selectionInput == selectionCounter + 1)
            {
                // If the user wants more results we move the cursor up and overwrite the next page option.
                // Then get the next page and recursively call NameSelectionMenu with the new response, 
                // current selection counter and, current selection lookup dictionary.
                // This will essentially append the next page results to the current page.
                Console.SetCursorPosition(0, Console.CursorTop - 2);
                FormattedResponse nextPageResponse = GetFormattedResponse(formattedResponse.ApiKey, formattedResponse.OriginalFileName, formattedResponse.CurrentPage + 1);
                NameSelectionMenu(nextPageResponse, selectionCounter, selectionLookup);
            }
            else
            {
                Console.WriteLine("Option selection:\n{0}", selectionLookup[selectionInput]);
            }

            // Return the selected name. If skipRename is true it means that the original name is being passed back
            return Tuple.Create(selectionLookup[selectionInput], skipRename);
        }

        public static FormattedResponse GetFormattedResponse(string apiKey, string fileName, int pageNumber = 1)
        {
            // TMDB will not return results for poorly formatted file names
            string formattedFileName = FormatFileNameForQuery(fileName);

            // Send request to TMDB and deserialize the response
            Response detailsDeserialized = JsonConvert.DeserializeObject<Response>(GetRestMethod(TMDB_SEARCH_URL + apiKey + "&query=" + formattedFileName + "&page=" + pageNumber));

            FormattedResponse formattedResponse = new FormattedResponse
            {
                Total_Results = detailsDeserialized.Total_Results,
                Total_Pages = detailsDeserialized.Total_Pages,
                FormattedFileName = formattedFileName,
                OriginalFileName = fileName,
                ApiKey = apiKey,
                CurrentPage = pageNumber,
                MovieTitles = new List<string>()
            };

            foreach (Movie movie in detailsDeserialized.Results)
            {
                // Formats as "Title (Date)"
                formattedResponse.MovieTitles.Add(FormatNewNameFromMovie(movie));
            }
            return formattedResponse;
        }

        

        public class Response
        {
            public int Total_Results { get; set; }
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

        public class FormattedResponse
        {
            public int Total_Results { get; set; }
            public int Total_Pages { get; set; }
            public int CurrentPage { get; set; }
            public string FormattedFileName { get; set; }
            public string OriginalFileName { get; set; }
            public string ApiKey { get; set; }
            public List<string> MovieTitles { get; set; }
        }
    }
}
