using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Savian.SaviDoc
{
    public enum StepType
    {
        Macro,
        Step,
        Section,
        Function,
        Unknown
    }

    public enum ParmType
    {
        In,
        Out,
    }

    public class SasProgram
    {
        public ProgramHeader Header { get; set; } = new ProgramHeader();
        public List<Step> Steps{ get; set; } = new List<Step>();
        public FileInfo ProgramInfo { get; set; }
        public string Program { get; set; }
        public string RootDir { get; internal set; }

        public class ProgramHeader
        {
            public string Company { get; set; }
            public string Location { get; set; }
            public string Name { get; set; }
            public string Author { get; set; }
            public string SasVersion { get; set; }
            public string Description { get; set; }
            public string Usage { get; set; }
            public string Remarks { get; set; }
            public List<Event> Events { get; set; } = new List<Event>();
        }
        public class Event
        {
            public string Description { get; set; }
            public string Id { get; set; }
            public string Date { get; set; }
        }
        public class Parm
        {
            public string Name { get; set; }
            public ParmType ParmType { get; set; }
            public string Description { get; set; }
            public string DefaultValue { get; set; }
        }
        public class Step
        {
            public string Name { get; set; }
            public StepType StepType { get; set; }
            public string Description { get; set; }
            public string Example { get; set; }
            public string SeeAlso { get; set; }
            public List<Parm> Parms { get; set; }
            public string AssociatedCode { get; set; }
            public int StartingLine { get; set; }
        }
    }
}
