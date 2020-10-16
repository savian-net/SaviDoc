using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using Serilog;
using static Savian.SaviDoc.SasProgram;

namespace Savian.SaviDoc
{
    class Program
    {
        public class Options
        {
            [Option('s', "source", Required = true, HelpText = "The source directory(s) that contain SAS programs. Separate with semicolons. It is required.", Separator = ';')]
            public IEnumerable<string> SourceDirs { get; set; }
            [Option('d', "doc", Required = true, HelpText = "The documentation directory where the output documentation will be stored. It is required.")]
            public string DocDir { get; set; }
            [Option('e', "excl", Required = false, HelpText = "Any directories that need to be excluded. Separate with semicolons.", Separator = ';')]
            public IEnumerable<string> ExclDirs { get; set; }
            [Option('u', "unattended", Required = false, HelpText = "A fla indicating whether the process should run without prompting at the end to press a key")]
            public bool Unattended { get; set; }
            [Option('l', "log", Required = false, HelpText = "This is a physical file where the log can be written.")]
            public string LogFile { get; set; }
        }

        public static string TagSetFileName { get; set; }
        public static string ErrorMessage { get; set; }
        public static List<SasProgram> SasPrograms { get; set; } = new List<SasProgram>();
        public static List<(string Name, string RootDir, string FileRef)> Docs { get; set; } = new List<(string Name, string RootDir, string FileRef)>();
        public static string _outDir;
        public static string _exeLocation = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        static void Main(string[] args)
        {
            var parser = new Parser();
            var result = parser.ParseArguments<Options>(args)
                        .WithParsed(opts => RunProgram(opts))
                        .WithNotParsed(errs => HandleParseErrors(errs))
                        ;

        }

        private static void HandleParseErrors(IEnumerable<Error> errs)
        {
            foreach (var err in errs)
            {
                Log.Error(err.ToString());
            }
        }

        private static void RunProgram(Options options)
        {
            Common.Initialize(options);

            Log.Information("Initializing engine for SAS programs.");
            foreach (var source in options.SourceDirs)
            {
                if (!Directory.Exists(source))
                {
                    Log.Error("Parameter is not a directory.");
                    return;
                }
                if (!Directory.Exists(options.DocDir))
                {
                    Log.Information("Output location was not found. Creating...");
                    Directory.CreateDirectory(options.DocDir);
                }
                _outDir = options.DocDir;
                var files = new DirectoryInfo(source).GetFiles("*.sas", SearchOption.AllDirectories).Where(p => !options.ExclDirs.Any(q => p.DirectoryName.ToLower().Contains(q.ToLower()))).ToList();
                Log.Information("Processing tagset...");
                Process(files);
            }
            Log.CloseAndFlush();
            if (!options.Unattended)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// The main entry point for SaviDoc. Takes the assigned system values and processes the SAS program.
        /// </summary>
        public static void Process(List<FileInfo> files)
        {
            foreach (var f in files)
            {
                Log.Information("Reading SAS program: " + f);
                ReadSasProgram(f.FullName);
                SasParser parser = new SasParser();
                Log.Information("Parsing code...");
                var pgm = parser.Parse(f.FullName);
                SasPrograms.Add(pgm);
            }
            Log.Information("Process HTML docs...");
            ProcessHtml();
            Log.Information("Create contents page...");
            CreateContentsPage();
            Log.Information("Completed.");
            Log.Information(new string('=', 100));
        }

        private static void CreateContentsPage()
        {
            var outHtml = Path.Combine(_outDir, "index.html");
            using (var sw = new StreamWriter(outHtml))
            {
                using (var sr = new StreamReader(Path.Combine(_exeLocation, "html/TemplateIndex.html")))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"<tr><th>SAS Program</th><th>Original Location</th><th>Document Location</th></tr>");
                    foreach (var doc in Docs)
                    {
                        sb.AppendLine($"<tr><td>{doc.Name}</td><td>{doc.RootDir}</td><td><a href='file:///{doc.FileRef}' target='_blank'>{doc.FileRef}</a></td></tr>");
                    }
                    var html = sr.ReadToEnd().Replace("[[CONTENTS]]", sb.ToString());
                    sw.Write(html);
                }
            }
        }

        private static void ProcessHtml()
        {
            var dirs = SasPrograms.Select(p => p.RootDir).Distinct();
            var dirInfo = new DirectoryInfo(_outDir);
            dirInfo.Clear();

            var commonPath = GetLongestCommonPath(dirs);

            foreach (var dir in dirs)
            {
                var pgms = SasPrograms.Where(p => p.RootDir == dir);
                foreach (var pgm in pgms)
                {
                    var root = dir.Replace(commonPath, string.Empty);
                    var outDirRoot = PathCombine(_outDir, root);
                    if (!Directory.Exists(outDirRoot))
                    {
                        Directory.CreateDirectory(outDirRoot);
                    }
                    var outHtml = Path.Combine(outDirRoot, pgm.Program + ".html");
                    Docs.Add((Name: pgm.Program, RootDir: pgm.RootDir, FileRef: outHtml));
                    using (var sw = new StreamWriter(outHtml))
                    {
                        using (var sr = new StreamReader(Path.Combine(_exeLocation, "html/Template.html")))
                        {
                            var html = ReplaceHtmlTags(pgm, sr.ReadToEnd());
                            sw.Write(html);
                        }
                    }
                }
            }
        }

        private static string GetLongestCommonPath(IEnumerable<string> files)
        {
            var MatchingChars =
                from len in Enumerable.Range(0, files.Min(s => s.Length)).Reverse()
                let possibleMatch = files.First().Substring(0, len)
                where files.All(f => f.StartsWith(possibleMatch))
                select possibleMatch;

            var longestDir = Path.GetDirectoryName(MatchingChars.First());
            return longestDir;
        }

        private static string ReplaceHtmlTags(SasProgram pgm, string v)
        {
            v = v.Replace("[[NAME]]", pgm.Program);
            v = v.Replace("[[PATH]]", pgm.RootDir);
            v = v.Replace("[[DESCRIPTION]]", pgm.Header.Description);
            v = v.Replace("[[LASTMODIFIED]]", pgm.ProgramInfo.LastWriteTime.ToString("yyyy-MMM-dd hh:mm:ss"));
            v = v.Replace("[[COMPANY]]", pgm.Header.Company);
            v = v.Replace("[[LOCATION]]", pgm.Header.Location);
            v = v.Replace("[[AUTHOR]]", pgm.Header.Author);
            v = v.Replace("[[SASVERSION]]", pgm.Header.SasVersion);
            v = v.Replace("[[USAGE]]", pgm.Header.Usage);

            v = InsertEventTable(pgm, v);
            v = InsertStepTable(pgm, v);
            return v;
        }

        private static string InsertEventTable(SasProgram pgm, string v)
        {

            var sb = new StringBuilder();
            sb.AppendLine("<table class='blueTable'>");
            sb.AppendLine("<tbody>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Id</th>");
            sb.AppendLine("<th>Date</th>");
            sb.AppendLine("<th>Description</th>");
            sb.AppendLine("</tr>");
            foreach (var e in pgm.Header.Events)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{e.Id}</td>");
                sb.AppendLine($"<td>{e.Date}</td>");
                sb.AppendLine($"<td>{e.Description}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
            return v.Replace("[[EVENTS]]", sb.ToString());
        }

        private static string InsertStepTable(SasProgram pgm, string v)
        {
            var sb = new StringBuilder();
            foreach (var s in pgm.Steps)
            {
                var props = typeof(Step).GetProperties().Where(p => p.Name != "AssociatedCode" && p.Name != "Parms");
                WriteNonListProperties(sb, props, s);

                props = typeof(Step).GetProperties().Where(p => p.Name == "Parms");

                WriteListProperties(sb, props, s);

                sb.AppendLine("<h5 style='margin-bottom:25px;'>Associated Code</h5>");
                sb.AppendLine("<pre style='margin-bottom: 25px;'><code>");
                var code = typeof(Step).GetProperty("AssociatedCode").GetValue(s).ToString();
                sb.Append(FixEntities(code));
                sb.AppendLine("</code></pre>");
            }
            return v.Replace("[[STEPS]]", sb.ToString());
        }

        private static string FixEntities(string code)
        {
            return code.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static void WriteListProperties(StringBuilder sb, IEnumerable<PropertyInfo> props, Step s)
        {
            foreach (var p in props)
            {
                var parms = p.GetValue(s) as List<Parm>;
                if (parms != null && parms.Any())
                {
                    sb.AppendLine($"<h6>{p.Name}</h6>");
                    sb.AppendLine("<table class='blueTable'>");
                    sb.AppendLine("<tbody>");
                    sb.AppendLine("<tr>");
                    sb.AppendLine("<th>Parameter</th>");
                    sb.AppendLine("<th>Type</th>");
                    sb.AppendLine("<th>Definition</th>");
                    sb.AppendLine("<th>Default</th>");

                    sb.AppendLine("</tr>");
                    foreach (var parm in parms)
                    {
                        sb.AppendLine($"<tr><td>{parm.Name}</td><td>{parm.ParmType}</td><td>{parm.Description}</td><td>{parm.DefaultValue}</td></tr>");
                    }
                }
            }
            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
        }

        private static void WriteNonListProperties(StringBuilder sb, IEnumerable<System.Reflection.PropertyInfo> props, Step s)
        {
            sb.AppendLine("<table class='blueTable' style='margin-bottom: 10px;'>");
            sb.AppendLine("<tbody>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Label</th>");
            sb.AppendLine("<th>Value</th>");
            sb.AppendLine("</tr>");
            foreach (var p in props)
            {
                var value = p.GetValue(s);
                if (value != null && !string.IsNullOrEmpty(value.ToString()))
                {
                    sb.AppendLine($"<tr><td>{p.Name}</td><td>{p.GetValue(s)}</td></tr>");
                }
            }
            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
        }

        private static string PathCombine(string path1, string path2)
        {
            if (Path.IsPathRooted(path2))
            {
                path2 = path2.TrimStart(Path.DirectorySeparatorChar);
                path2 = path2.TrimStart(Path.AltDirectorySeparatorChar);
            }

            return Path.Combine(path1, path2);
        }

        private static void ReadSasProgram(string sasProgram)
        {
            try
            {
                FileInfo fi = new FileInfo(sasProgram);
                if (fi.Exists)
                {
                    StreamReader sr = new StreamReader(sasProgram);
                    sasProgram = sr.ReadToEnd();
                }
                else
                {
                    Log.Error("Unable to find the SAS program specified. The file was " + sasProgram);
                }

            }
            catch (Exception ex)
            {
                Log.Error("Unable to read the SAS program.", ex);
            }
        }
    }
}
