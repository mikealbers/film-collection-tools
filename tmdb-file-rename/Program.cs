using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace tmdb_file_rename
{
    class Program
    {
        public const string TMDB_SEARCH_URL = "https://api.themoviedb.org/3/search/movie?api_key=";
        public const string OUTPUT_FILE_NAME = "TMDB-skipped-files.txt";
        public const string MANUAL_RENAME_DIRECTORY = "ManualRename";

        public const string TITLE = 
                "----------------------------------------\n" +
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
            string apiKey;
            string directoryPath;

            Console.WriteLine(TITLE);

            if (args.Length == 2)
            {
                apiKey = args[0];
                directoryPath = args[1];
            } 
            else
            {
                Console.WriteLine("Enter a TMDB api key:");
                apiKey = Console.ReadLine();
                Console.WriteLine("Enter a directory path:");
                directoryPath = Console.ReadLine();
            }

            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            FileInfo[] files = directory.GetFiles();

            if (files.Length == 0)
            {
                do
                {
                    Console.WriteLine("Directory contains no files. Enter a new directory path:");
                    directoryPath = Console.ReadLine();
                    directory = new DirectoryInfo(directoryPath);
                    files = directory.GetFiles();
                }
                while (files.Length == 0);
            }

            Tuple<List<string>, List<string>> selectedAndSkippedNames = GetSelectedAndSkippedNamesFromUser(files, apiKey);

            // Print out all of the user selections
            // including the automatically skipped file names
            Console.WriteLine("----------------------------------------\n" +
                "----------------------------------------\n" +
                "The files in this directory will be renamed as followed:");
            foreach (string selectedName in selectedAndSkippedNames.Item1)
            {
                Console.WriteLine(selectedName);
            }
            Console.WriteLine("----------------------------------------\n" +
                "----------------------------------------\n" +
                "Confirm rename ( y / n )");

            ConsoleKey userResponse = GetUserConfirmation();

            if (userResponse == ConsoleKey.Y)
            {
                Console.WriteLine("Would you like to move the skipped files into {0}? ( y / n )\n", MANUAL_RENAME_DIRECTORY);
                userResponse = GetUserConfirmation();
                if (userResponse == ConsoleKey.Y)
                {
                    CreateManualRenameDirectory(directoryPath + @"\" + MANUAL_RENAME_DIRECTORY);
                    RenameFiles(files, selectedAndSkippedNames.Item1, true);
                }
                else
                {
                    RenameFiles(files, selectedAndSkippedNames.Item1);
                }

                WriteSkippedListToTextFile(directoryPath + @"\" + OUTPUT_FILE_NAME, selectedAndSkippedNames.Item2);

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
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
            Encoding encoding = Encoding.GetEncoding("utf-8");
            StreamReader responseStream = new StreamReader(webResponse.GetResponseStream(), encoding);
            string result = responseStream.ReadToEnd();
            webResponse.Close();
            return result;
        }

        public static void WriteSkippedListToTextFile(string outputPath, List<string> skippedNameList)
        {
            TextWriter textWriter = new StreamWriter(outputPath);
            foreach (String skippedName in skippedNameList)
            {
                textWriter.WriteLine(skippedName);
            }
            textWriter.Close();
        }

        public static void CreateManualRenameDirectory(string newDirectoryPath)
        {
            try
            {
                Console.WriteLine("Creating {0}", MANUAL_RENAME_DIRECTORY);
                // Determine whether the directory exists.
                if (Directory.Exists(newDirectoryPath))
                {
                    Console.WriteLine("That directory exists already");
                    return;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(newDirectoryPath);
                Console.WriteLine("The directory was created successfully at {0} on {1}.", newDirectoryPath, Directory.GetCreationTime(newDirectoryPath));
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
        }

        public static void RenameFiles(FileInfo[] files, List<string> newFileNames, bool moveSkippedFiles = false)
        {
            int fileCount = 0;
            Regex illegalInFileName = new Regex(@"[\\/:*?""<>|]");
            
            foreach (FileInfo file in files)
            {
                if (fileCount < newFileNames.Count)
                {
                    if (Path.GetFileNameWithoutExtension(file.FullName) != newFileNames[fileCount])
                    {
                        try
                        {
                            File.Move(file.FullName, file.FullName.Replace(Path.GetFileNameWithoutExtension(file.FullName), illegalInFileName.Replace(newFileNames[fileCount], "")));
                        }
                        catch(Exception exception)
                        {
                            Console.WriteLine(exception);
                        }
                    }
                    else if (Path.GetFileNameWithoutExtension(file.FullName) == newFileNames[fileCount] && moveSkippedFiles)
                    {
                        var path = Path.GetDirectoryName(file.FullName) + @"\" + MANUAL_RENAME_DIRECTORY + @"\" + Path.GetFileName(file.FullName);
                        File.Move(file.FullName, path);
                    }
                    fileCount++;
                }
            }
        }

        public static ConsoleKey GetUserConfirmation()
        {
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

            //TODO: Remove. Commented out to skip already formatted files
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
                    Console.WriteLine("[{0} results] for: {1}\n" +
                        "Please choose from one of the following:\n" +
                        "----------------------------------------", formattedResponse.Total_Results, file.FullName);
                    Tuple<string, bool, bool> selection = NameSelectionMenu(formattedResponse);

                    selectedNames.Add(selection.Item1);

                    // Keep track of all skipped files to be able to print out at end
                    if (selection.Item2) skippedFiles.Add(selection.Item1);

                    // Quit early and go to rename confirmation
                    if (selection.Item3) return Tuple.Create(selectedNames, skippedFiles);
                }
            }
            return Tuple.Create(selectedNames,skippedFiles);
        }

        public static Tuple<string, bool, bool> NameSelectionMenu(FormattedResponse formattedResponse, int selectionCounter = 0, Dictionary<int, string> selectionLookup = null)
        {
            int selectionInput;
            int maximumSelectionInput;
            bool skipRename = false;
            bool quitEarly = false;

            // Only add skip option on first call
            if (selectionCounter == 0 && selectionLookup == null)
            {
                selectionLookup = new Dictionary<int, string>();
                Console.WriteLine("0.) SKIP rename or quit early");
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
                maximumSelectionInput = selectionCounter + 1;
                Console.WriteLine("{0}.) Next page", selectionCounter + 1);
            }
            else
            {
                // Effects the input filter.
                maximumSelectionInput = selectionCounter;
            }

            // Input filter to remove non numbers and anything outside the selection range
            while (!int.TryParse(Console.ReadLine(), out selectionInput) || selectionInput > maximumSelectionInput)
            {
                Console.WriteLine("Invalid selection \n" +
                    "Enter a value between 0 and {0}", maximumSelectionInput);
            }

            if (selectionInput == 0)
            {
                Console.WriteLine("Skipping rename\n");
                skipRename = true;

                Console.WriteLine("Would you like to quit early? ( y / n )");
                ConsoleKey userResponse = GetUserConfirmation();

                if (userResponse == ConsoleKey.Y)
                {
                    Console.WriteLine("Exiting to confirmation of currently selected names.");
                    quitEarly = true;
                    return Tuple.Create(formattedResponse.FormattedFileName, skipRename, quitEarly);
                }
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
            return Tuple.Create(selectionLookup[selectionInput], skipRename, quitEarly);
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
