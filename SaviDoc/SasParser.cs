using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Savian.SaviDoc.SasProgram;

namespace Savian.SaviDoc
{
    internal class SasParser
    {
        SasProgram pgm;
        internal SasProgram Parse(string sasProgram)
        {
            pgm = new SasProgram();
            GetCommentSections(sasProgram);
            return pgm;
        }

        private void GetCommentSections(string sasProgram)
        {
            try
            {
                var pattern = Common.GetPattern("General", "Comment");
                var code = new StreamReader(sasProgram).ReadToEnd();
                var matches = pattern.Matches(code);
                var fi = new FileInfo(sasProgram);
                pgm.ProgramInfo = fi;
                pgm.Program = fi.Name.Split('.')[0];
                pgm.RootDir = fi.DirectoryName;

                foreach (Match match in matches)
                {

                    if (match.IsHeaderComment())
                    {
                        GetHeader(match, sasProgram);
                    }
                    else
                    {
                        ParseSteps(match, code);
                    }
                }
                if (matches.Count == 0)
                {
                    
                    pgm.Steps.Add(new Step(){ AssociatedCode = code}); 
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to find comment section.");
            }
        }

        #region Header

        public void GetHeader(Match match, string sasProgram)
        {
            var parmValues = GetParmValues(match);
            pgm.Header.Author = GetParm(parmValues, "AUTHOR", true); 
            pgm.Header.Company = GetParm(parmValues, "COMPANY"); 
            pgm.Header.Description = GetParm(parmValues, "DESCRIPTION", true); 
            pgm.Header.Location = GetParm(parmValues, "LOCATION"); 
            pgm.Header.Name = GetParm(parmValues, "NAME"); 
            pgm.Header.Remarks = GetParm(parmValues, "REMARKS"); 
            pgm.Header.SasVersion = GetParm(parmValues, "SASVERSION"); 
            pgm.Header.Usage = GetParm(parmValues, "USAGE", true); 
            DetermineEvents(parmValues.Where(p => p.Parm == "EVENT").ToList());
        }

        private string GetParm(List<(string Parm, string Value)> parmValues, string name, bool join = false)
        {
            var value = parmValues.Where(p => p.Parm.StartsWith(name)).Select(p => p.Value?.Trim());
            if (join)
            {
                return string.Join(';',value);
            }
            return value.FirstOrDefault() ;
        }

        private List<(string Parm, string Value)> GetParmValues(Match match)
        {
            var lines = Regex.Split(match.Value, "\r\n|\r|\n");
            var lastParm = string.Empty;
            var pairs = new List<(string Parm, string Value)>();
            try
            {

                foreach (var line in lines)
                {
                    var value = string.Empty;
                    var values = line.Split(':');
                    var parm = values[0].ToUpper().Trim().Replace(" ", string.Empty);
                    if (parm.StartsWith("/*") || parm.EndsWith("*/"))
                        continue;
                    if (values.Length > 1)
                    {
                        value = values[1];
                    }
                    if (parm != string.Empty)
                    {
                        pairs.Add((Parm: parm, Value: value));
                        lastParm = parm;
                    }
                    else
                    {
                        pairs.Add((Parm: lastParm, Value: value));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error parsing header");
            }
            return pairs;
        }

        private void DetermineEvents(List<(string Parm, string Value)> parmValues)
        {
            try
            {
                foreach (var rec in parmValues)
                {
                    var values = rec.Value.Split('|');
                    Event e = new Event
                    {
                        Description = values.Length > 2 ? values[2].Trim() : string.Empty,
                        Date = values.Length > 1 ? values[1].Trim() : string.Empty,
                        Id = values[0].Trim()
                    };
                    pgm.Header.Events.Add(e);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to determine events.");
            }
        }
        #endregion Header

        #region StepParser

        void ParseSteps(Match match, string code)
        {
            try
            {
                var line = match.GetLineNumber(code.Substring(0, match.Index));
                var parmValues = GetParmValues(match);
                //DetermineCommentLocations(match, code);

                var step = new Step();
                step.StartingLine = line;
                step.AssociatedCode = GetCodeForCommentBlock(match, code);
                step.Example = parmValues.FirstOrDefault(p => p.Parm.StartsWith("EXAMPLE")).Value.GetValue().Trim();
                step.Description = string.Join(';', (parmValues.Where(p => p.Parm.StartsWith("DESCRIPTION")).Select(p => p.Value.GetValue().Trim())));
                step.SeeAlso = string.Join(';', (parmValues.Where(p => p.Parm.StartsWith("SEEALSO")).Select(p => p.Value.GetValue().Trim())));
                step.StepType = DetermineStepType(parmValues, step);
                step.Parms = DetermineParms(parmValues.Where(p => p.Parm == "IN" || p.Parm == "OUT" || p.Parm == "PARM").ToList());
                pgm.Steps.Add(step);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to parse data.");
            }
        }

        private StepType DetermineStepType(List<(string Parm, string Value)> parmValues, Step step)
        {
            if (parmValues.Any(p => p.Parm == "MACRO"))
            {
                step.Name = parmValues.FirstOrDefault(p => p.Parm.StartsWith("MACRO")).Value.GetValue().Trim();
                return StepType.Macro;
            }
            if (parmValues.Any(p => p.Parm == "SECTION"))
            {
                step.Name = parmValues.FirstOrDefault(p => p.Parm.StartsWith("SECTION")).Value.GetValue().Trim();
                return StepType.Section;
            }
            if (parmValues.Any(p => p.Parm == "STEP"))
            {
                step.Name = parmValues.FirstOrDefault(p => p.Parm.StartsWith("STEP")).Value.GetValue().Trim();
                return StepType.Step;
            }
            if (parmValues.Any(p => p.Parm == "FUNCTION"))
            {
                step.Name = parmValues.FirstOrDefault(p => p.Parm.StartsWith("STEP")).Value.GetValue().Trim();
                return StepType.Function;
            }
            step.Name = "N/A";
            return StepType.Unknown;
        }

        private List<Parm> DetermineParms(List<(string Parm, string Value)> parmValues)
        {
            try
            {
                List<Parm> parms = new List<Parm>();
                foreach (var rec in parmValues)
                {
                    var values = rec.Value.Split('|');
                    var e = new Parm
                    {
                        Name = values[0],
                        Description = values.Length > 1 ? values[1].Trim() : string.Empty,
                        DefaultValue = values.Length > 2 ? values[2].Trim() : string.Empty,
                        ParmType = rec.Parm == "IN" ? ParmType.In : ParmType.Out,
                    };
                    parms.Add(e);
                }
                return parms;
            }
            catch (Exception ex)
            {
                Log.Error("Unable to determine parms.", ex);
                return null;
            }
        }

        #endregion StepParser

        #region Utility
        /// <summary>
        /// Determines the code that the comment block precedes 
        /// </summary>
        /// <param name="match">The step being checked.</param>
        /// <param name="code">The raw SAS code</param>
        protected string GetCodeForCommentBlock(Match match, string code)
        {
            string commentBlockCode = string.Empty;
            try
            {
                Match next = match.NextMatch();
                if (next != null && next.Index != 0)
                {
                    var start = match.Index + match.Length;
                    var length = next.Index - match.Index - match.Length;
                    commentBlockCode = code.Substring(start, length).Trim(new[] { ' ', '\r', '\n', '\t' });
                }
                else if (next.Index == 0)
                {
                    var start = match.Index + match.Length;
                    var length = code.Length - match.Index - match.Length;
                    commentBlockCode = code.Substring(start, length).Trim(new[] { ' ', '\r', '\n', '\t' });
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unable to determine comment locations.", ex);
            }
            return commentBlockCode;
        }

        #endregion Utility;
    }
}
