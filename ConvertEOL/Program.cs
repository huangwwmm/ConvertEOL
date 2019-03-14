using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertEOL
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            if (args == null || args.Length < 1)
            {
                args = new string[] { "--pathfile", "D:\\Path.txt", "--extensionw", ".cs", "--eol", "0" };
            }
#endif
            AppDomain.CurrentDomain.UnhandledException += OnException;

            ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);
            switch (result.Tag)
            {
                case ParserResultType.Parsed:
                    Parsed<Options> parsed = (Parsed<Options>)result;
                    Action action = new Action(parsed.Value);
                    break;
                case ParserResultType.NotParsed:
                default:
                    Options template = new Options();
                    template.Directory = "D:\\";
                    template.PathsFile = "D:\\Path.txt";
                    template.ExtensionWhiteList = new string[] { ".c", ".h", ".cpp" };
                    template.ExtensionBlackList = new string[] { ".txt", ".png" };
                    template.EOL = 0;
                    Console.WriteLine("Example:");
                    Console.WriteLine("\tbash " + Parser.Default.FormatCommandLine<Options>(template));
                    break;
            }
            Console.ReadKey();
        }

        private static void OnException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.ReadKey();
            Environment.Exit(1);
        }
    }

    public class Action
    {
        public readonly static string[] EOLs = new string[] { "\n", "\r\n" };

        public Action(Options options)
        {
            List<FileInfo> willConvertFiles = new List<FileInfo>();
            if (!string.IsNullOrEmpty(options.Directory))
            {
                OpenOrCreateDirectory(options.Directory, false);
                string[] allFiles = Directory.GetFiles(options.Directory, "*", SearchOption.AllDirectories);
                FilterFile(options, allFiles, ref willConvertFiles);
            }

            if (!string.IsNullOrEmpty(options.PathsFile))
            {
                string[] allFiles = File.ReadAllLines(options.PathsFile);
                FilterFile(options, allFiles, ref willConvertFiles);
            }

            int convertedFileCount = 0;
            string targetEOL = EOLs[options.EOL];
            for (int iFile = 0; iFile < willConvertFiles.Count; iFile++)
            {
                FileInfo iterFile = willConvertFiles[iFile];
                try
                {
                    string text = File.ReadAllText(iterFile.FullName);
                    string convertedText = text.Replace("\r\n", "\n").Replace("\n", targetEOL);

                    if (convertedText != text)
                    {
                        convertedFileCount++;
                        File.WriteAllText(iterFile.FullName, convertedText);
                        Console.WriteLine("Success converted file: " + iterFile.FullName);
                    }
                    else
                    {
                        Console.WriteLine("Ignore file: " + iterFile.FullName);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(string.Format("Converted file {0} failed, Exception:\n{1}", iterFile.FullName, e.ToString()));
                }
            }
            Console.WriteLine(string.Format("Converted {0} files", convertedFileCount));
        }

        public void OpenOrCreateDirectory(string directory, bool createIfNotFound)
        {
            if (!Directory.Exists(directory))
            {
                if (createIfNotFound)
                {
                    Directory.CreateDirectory(directory);
                }
                if (!Directory.Exists(directory))
                {
                    throw new DirectoryNotFoundException(string.Format("Directory {0} not found and can't create", directory));
                }
            }
        }

        public void FilterFile(Options options, string[] files, ref List<FileInfo> willConvertFiles)
        {
            HashSet<string> extensionWhiteList = options.ExtensionWhiteList == null
               ? new HashSet<string>()
               : new HashSet<string>(options.ExtensionWhiteList);
            bool allExtension = extensionWhiteList.Contains("*");
            HashSet<string> extensionBlackList = options.ExtensionBlackList == null
                ? new HashSet<string>()
                : new HashSet<string>(options.ExtensionBlackList);
            for (int iFile = 0; iFile < files.Length; iFile++)
            {
                string iterFile = files[iFile];
                if (string.IsNullOrEmpty(iterFile)
                    || !File.Exists(iterFile))
                {
                    continue;
                }
                FileInfo iterFileInfo = new FileInfo(iterFile);
                if ((allExtension || extensionWhiteList.Contains(iterFileInfo.Extension))
                    && !extensionBlackList.Contains(iterFileInfo.Extension))
                {
                    willConvertFiles.Add(iterFileInfo);
                }

                Console.WriteLine("Ignore file: " + iterFileInfo.FullName);
            }
        }
    }

    public class Options
    {
        [Option("dir"
            , Required = false
            , Default = null
            , HelpText = "Will convert all files in this directory")]
        public string Directory { get; set; }

        [Option("pathfile"
            , Required = false
            , Default = null
            , HelpText = "Convert all files recorded in this file")]
        public string PathsFile { get; set; }

        [Option("extensionw"
            , Default = new string[] { "*" }
            , HelpText = "Extension White List")]
        public IEnumerable<string> ExtensionWhiteList { get; set; }

        [Option("extensionb"
            , Default = null
            , HelpText = "Extension Black List")]
        public IEnumerable<string> ExtensionBlackList { get; set; }

        [Option("eol"
           , Default = false
           , HelpText = "EOL: 0-\\n 1-\\r\\n")]
        public int EOL { get; set; }
    }
}
