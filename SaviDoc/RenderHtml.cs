using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Savian.SaviDoc
{
    class RenderHtml
    {
        List<string> dirs = new List<string>();
        Dictionary<FileInfo, string> dirFiles = new Dictionary<FileInfo, string>();
        List<Comment> allCodeComments = new List<Comment>();


        private string compressedDirectory;

        internal string Directory  { get; set; }
        internal List<FileInfo> Files { get; set; }

        public RenderHtml(string dir, ref List<FileInfo> files)
        {
            Directory = dir + @"\SasCodeDocumentation";
            Files = files;

            CreateDirectory();

            DetermineDirectories();
            DetermineFilesInDirectories();

            RenderDirectoryPage();
            RenderBannerPage();
            AddGraphicElements();
            RenderIndividualPages();
            DialogResult dr = MessageBox.Show("Documentation has been created at " + Environment.NewLine + dir + @"\Documentation.htm" + Environment.NewLine + Environment.NewLine +
                                              "Would you like to open it now?",
                                              "Documentation Created",
                                              MessageBoxButtons.YesNo);
            if (dr == DialogResult.Yes && File.Exists(dir + @"\SasCodeDocumentation\Documentation.htm"))
            {
                Process.Start(dir + @"\SasCodeDocumentation\Documentation.htm");
            }

        }

        private void DetermineDirectories()
        {
            foreach (FileInfo fi in Files)
            {
                if (!dirs.Contains(fi.DirectoryName))
                    dirs.Add(fi.DirectoryName);
            }
        }

        private void AddGraphicElements()
        {
            SaveResourceToFile("banner.jpg", Directory + @"\banner.jpg");
            SaveResourceToFile("commentreport.css", Directory + @"\commentreport.css");
            SaveResourceToFile("darkcorner.jpg", Directory + @"\darkcorner.jpg");
            SaveResourceToFile("gradleft.jpg", Directory + @"\gradleft.jpg");
            SaveResourceToFile("gradtop.jpg", Directory + @"\gradtop.jpg");
            SaveResourceToFile("bluecorner.jpg", Directory + @"\bluecorner.jpg");
            SaveResourceToFile("graycorner.jpg", Directory + @"\graycorner.jpg");
            SaveResourceToFile("minus.jpg", Directory + @"\minus.jpg");
            SaveResourceToFile("plus.jpg", Directory + @"\plus.jpg");
            SaveResourceToFile("titletile.jpg", Directory + @"\titletile.jpg");
            SaveResourceToFile("vt.js", Directory + @"\vt.js");
        }

        private void SaveResourceToFile(string strResource, string strFileName)
        {
            // Get embedded resource item. 
            Stream stm = GetType().Module.Assembly.GetManifestResourceStream("SaviDoc.HtmlElements." + strResource);
            string[] test = GetType().Module.Assembly.GetManifestResourceNames();

            // Resource item exists. 
            if (stm != null)
            {
                FileInfo fi = new FileInfo(@strFileName);
                if (!fi.Exists)
                {
                    // Create byte array and read resource file into it. 
                    byte[] bytes = new byte[stm.Length];
                    BinaryReader br = new BinaryReader(stm);
                    br.Read(bytes, 0, bytes.Length);

                    // Create file on disk drive and write byte array into it. 
                    FileStream fs = new FileStream(@strFileName, FileMode.CreateNew);
                    BinaryWriter bw = new BinaryWriter(fs);
                    bw.Write(bytes, 0, bytes.Length);

                    // Clean up. 
                    bw.Flush();
                    bw.Close();
                }
            }
        }

        private void RenderBannerPage()
        {
            StreamWriter sw = new StreamWriter(Directory + @"\banner.htm");
            sw.WriteLine("<HTML>");
            sw.WriteLine("<BODY BGCOLOR=\"#6699ff\" topmargin=0 rightmargin=0 leftmargin=0 bottommargin=0 scroll=\"no\">");
            sw.WriteLine("<IMG SRC=\"Banner.jpg\">");
            sw.WriteLine("<font face=\"Tahoma, Arial, Helvetica\" size=2 >");
            sw.WriteLine("<h2><font color=\"Black\" style=\"position:absolute; left:100px; top:12px;\">SaviDoc Code Documentation</font></h2>");
            sw.WriteLine("</font>");
            sw.WriteLine("</BODY>");
            sw.WriteLine("</HTML>");
            sw.Close();
        }

        private void RenderIndividualPages()
        {
            string modDir;
            foreach (string str in dirs)
            {
                modDir = str.Replace(prop.Default.DirectoryToSearch, "");
                compressedDirectory = GetCompressedFileName(modDir);
                DirectoryInfo di = new DirectoryInfo(Directory + @"\" + compressedDirectory);
                if (!di.Exists)
                {
                    di.Create();
                }

                RenderIndividualDirectoryPage(compressedDirectory);
                RenderLeftFrame(str);
                RenderRightFrame(str);
            }
        }

        private void RenderLeftFrame(string str)
        {
            StreamWriter sw = new StreamWriter(Directory + @"\" + compressedDirectory + @"\CWP0.htm");
            sw.WriteLine("<HTML>");
            sw.WriteLine("<HEAD>");
            sw.WriteLine("<META http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
            sw.WriteLine("<TITLE>" + str + "</TITLE>");
            sw.WriteLine("<LINK REL=STYLESHEET HREF=\"../CommentReport.css\" TYPE=\"text/css\">");
            sw.WriteLine("<SCRIPT language=\"JavaScript\" src=\"../VT.js\"></SCRIPT>");
            sw.WriteLine("</HEAD>");
            sw.WriteLine("<BODY topmargin=0 rightmargin=0 leftmargin=0 style=\"background-image: url(../titletile.jpg); background-repeat:repeat-x; background-position: 0 0;\" onload=\"InitElements()\">");
            sw.WriteLine("<DIV CLASS=\"linkbuttons\">");
            sw.WriteLine("<SPAN onselectstart=\"event.returnValue=false;\"");
            sw.WriteLine("onmouseover=\"this.style.color='#003366'; this.style.cursor='default';this.style.cursor='hand'; this.style.textDecoration='underline'\"");
            sw.WriteLine("onmouseout=\"this.style.color='#003366'; this.style.textDecoration='none'\"");
            sw.WriteLine("onClick=\"parent.location='../Documentation.htm'\" style=\"position:relative; left:7; color='#003366';\">");
            sw.WriteLine("All");
            sw.WriteLine("</SPAN>");

            sw.WriteLine("</DIV>");
            int i = 0;
            foreach (KeyValuePair<FileInfo, string> kvp in dirFiles)
            {
                if (kvp.Value == str)
                {
                    sw.WriteLine("<DIV CLASS=\"Namespace\">");
                    sw.WriteLine("<IMG name=\"sysTimerIcon\" src=\"../plus.jpg\" onClick=\"gvResolveX2(EL0" + i + ", sysTimerIcon, '../')\" style=\"display:none;\">");
                    sw.WriteLine("<SPAN");
                    sw.WriteLine("onselectstart=\"event.returnValue=false;\"");
                    sw.WriteLine("onmouseover=\"this.style.color='003399';this.style.cursor='default';this.style.cursor='hand';this.style.textDecoration='underline'\"");
                    sw.WriteLine("onmouseout=\"this.style.color='#003399';this.style.textDecoration='none'\"");
                    sw.WriteLine("onClick=\"gvResolveX2(EL0" + i + ", sysTimerIcon, '../')\" style=\"position:relative; left:4;\">");
                    sw.WriteLine(kvp.Key.Name);
                    sw.WriteLine("</SPAN>");
                    sw.WriteLine("</DIV>");
                    AddCodeSections(ref sw, i, kvp.Key.FullName) ; // + @"\" + kvp.Key);
                    i++;
                }
            }
            sw.WriteLine("</BODY>");
            sw.WriteLine("</HTML>");
            sw.Close();
        }

        private void RenderRightFrame(string str)
        {
            StreamWriter sw = new StreamWriter(Directory + @"\" + compressedDirectory + @"\CWP2.htm");
            sw.WriteLine("<HTML>");
            sw.WriteLine("<HEAD>");
            sw.WriteLine("<META http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
            sw.WriteLine("<TITLE></TITLE>");
            sw.WriteLine("<LINK REL=STYLESHEET HREF=\"../CommentReport.css\" TYPE=\"text/css\">");
            sw.WriteLine("</HEAD>");
            sw.WriteLine("<BODY topmargin=0 rightmargin=0 leftmargin=0 style=\"background-image: url(../titletile.jpg); background-repeat:repeat-x; background-position: 0 0;\" >");
            sw.WriteLine("<DIV CLASS=\"PageHeading\">" + compressedDirectory + "</DIV>");
            sw.WriteLine("<DIV CLASS=\"Description\">");
            sw.WriteLine("On the left is the list of programs found in this directory. Clicking on the expansion indicator '+' will show the code segments found within that program, in order of appearance.");
            sw.WriteLine("Clicking on one of the code segments will show the detailed information for that code segment in this pane.");
            sw.WriteLine("</DIV>");
            sw.WriteLine("<IMG src=\"../GradLeft.jpg\" width=7 height=378 alt=\"\" border=\"0\" style=\"position:absolute; left:10; top:18;z-Index:2\">");
            sw.WriteLine("<IMG src=\"../GradTop.jpg\" width=352 height=7 alt=\"\" border=\"0\" align=\"top\" style=\"position:absolute; left:10; top:18; z-index:1\">");
            sw.WriteLine("</BODY>");
            sw.WriteLine("</HTML>");
            sw.Close();
        }

        private void AddCodeSections(ref StreamWriter sw, int i, string sasProgramFile)
        {
            ParseComments parse = new ParseComments(sasProgramFile);
            List<CodeComment> codeComments = parse.CodeComments;

            allCodeComments.AddRange(codeComments);
            FileInfo fi = new FileInfo(sasProgramFile);
            sasProgramFile = fi.Name.Replace(fi.Extension, "");
            string parentDir = CreateDirectoryLocation(sasProgramFile);
            sw.WriteLine("<DIV CLASS=\"NamespaceChild\" id=\"EL0" + i + "\" style=\"display:block; position:relative; left:34px;\">");
            if (parse.ProgramHeader != null)
            {
                RenderDivSectionForProgramHeader(ref sw, sasProgramFile, parse.ProgramHeader);
                RenderProgramHeaderPage(parentDir, parse.ProgramHeader);
            }

            foreach (Comment cc in codeComments)
            {
                string color = ""; // SelectColorBasedUponType(cc);
                sw.WriteLine("<DIV title='" + cc.Remarks + "' style=\"color='#" + color + "'\" ");
                sw.WriteLine("onselectstart=\"event.returnValue=false;\"");
                sw.WriteLine("onmouseover=\"this.style.color='#" + color + "';this.style.cursor='default';this.style.cursor='hand';this.style.textDecoration='underline'\"");
                sw.WriteLine("onmouseout=\"this.style.color='#" + color + "';this.style.textDecoration='none'\"");
                sw.WriteLine("onClick=\"parent.CNTFRAME.location='" + sasProgramFile + "%5C" + cc.LinkFile + "';\">");
                sw.WriteLine(cc.Name);
                sw.WriteLine("</DIV>");
                RenderIndividualCommentPages(parentDir, cc);
            }
            sw.WriteLine("</DIV>");
        }

        private void RenderDivSectionForProgramHeader(ref StreamWriter sw, string sasProgramFile, Header hdr)
        {
            string color = "000099";
            sw.WriteLine("<DIV title='" + hdr.Description + "' style=\"color='#" + color + "';font-style:italic ;\" ");
            sw.WriteLine("onselectstart=\"event.returnValue=false;\"");
            sw.WriteLine("onmouseover=\"this.style.color='#" + color + "';this.style.cursor='default';this.style.cursor='hand';this.style.textDecoration='underline'\"");
            sw.WriteLine("onmouseout=\"this.style.color='#" + color + "';this.style.textDecoration='none'\"");
            sw.WriteLine("onClick=\"parent.CNTFRAME.location='" + sasProgramFile + "%5CprogramHeader.htm';\">");
            sw.WriteLine("Program Header");
            sw.WriteLine("</DIV>");
        }

        //private string SelectColorBasedUponType(Comment cc)
        //{
        //    switch (cc.TypeOfCode.ToLower())
        //    {
        //        case "macro":
        //            return "000099";
        //        case "data step":
        //        case "datastep":
        //            return "990000";
        //        default:
        //            return "FF0000";
        //    }
        //}

        private string CreateDirectoryLocation(string sasProgramFile)
        {
            StringBuilder parentDir = new StringBuilder(Directory).Append(@"\").Append(compressedDirectory).Append(@"\").Append(sasProgramFile).Append(@"\");
            DirectoryInfo di = new DirectoryInfo(parentDir.ToString());
            if (!di.Exists)
                di.Create();
            return parentDir.ToString();
        }

        private void DetermineFilesInDirectories()
        {
            foreach (FileInfo fi in Files)
            {
                dirFiles.Add(fi, fi.DirectoryName);
            }
        }

        private void RenderIndividualDirectoryPage(string str)
        {
            StreamWriter sw = new StreamWriter(Directory + @"\" + compressedDirectory + @"\" + compressedDirectory + ".htm");
            sw.WriteLine("<HTML>");
            sw.WriteLine("<HEAD>");
            sw.WriteLine("<META http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
            sw.WriteLine("<TITLE>" + str + "</TITLE>");
            sw.WriteLine("<LINK REL=STYLESHEET HREF=\"../CommentReport.css\" TYPE=\"text/css\">");
            sw.WriteLine("</HEAD>");
            sw.WriteLine("<FRAMESET ROWS=\"45, *\" BORDER=0>");
            sw.WriteLine("<FRAME SRC=\"../Banner.htm\">");
            sw.WriteLine("<FRAMESET COLS=\"190,*\" BORDER=0>");
            sw.WriteLine("<FRAME NAME=\"BARFRAME\" SRC=\"CWP0.HTM\">");
            sw.WriteLine("<FRAME NAME=\"CNTFRAME\" SRC=\"CWP2.HTM\">");
            sw.WriteLine("</FRAMESET>");
            sw.WriteLine("</FRAMESET>");
            sw.WriteLine("</HTML>");
            sw.Close();
        }

        private string GetCompressedFileName(string modDir)
        {
            string compressedDirectory = modDir.TrimStart('\\').Replace(@"\", "_").Replace(" ", "");
            if (compressedDirectory == "")
                compressedDirectory = "root";
            return compressedDirectory;
        }

        private void RenderDirectoryPage()
        {
            string modDir, compressedDirectory;
            DirectoryInfo di;

            StreamWriter sw = new StreamWriter(Directory + @"\Documentation.htm");
            sw.WriteLine("<HTML>");
            sw.WriteLine("<HEAD>");
            sw.WriteLine("<META http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
            sw.WriteLine("<TITLE>SaviDoc " + prop.Default.DirectoryToSearch + "</TITLE>");
            sw.WriteLine("<LINK REL=STYLESHEET HREF=\"CommentReport.css\" TYPE=\"text/css\">");
            sw.WriteLine("</HEAD>");
            sw.WriteLine("<BODY>");
            sw.WriteLine("<TABLE CELLPADDING=\"0\" CELLSPACING=\"0\" width=\"100%\"><TR><TD BGCOLOR=\"#6699ff\"><IMG SRC=\"Banner.jpg\">");
            sw.WriteLine("</TD></TR><TR><TD><FONT face=\"Tahoma, Arial, Helvetica\" size=2 ><H2>");
            sw.WriteLine("<FONT color=\"Black\" style=\"position:absolute; left:100px; top:12px;\">SaviDoc Documentation</FONT></H2></FONT></TD></TR></TABLE>");
            sw.WriteLine("<DIV CLASS=\"PageHeading\">SAS Code Documentation for " + prop.Default.DirectoryToSearch + "</DIV>");
            sw.WriteLine("<DIV CLASS=\"Description\">");
            sw.WriteLine("");
            sw.WriteLine("</DIV>");
            sw.WriteLine("<TABLE CLASS=\"InfoTable\" cellpadding=0 cellspacing=0>");
            sw.WriteLine("<TR height=20>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("<TD valign=top align=left width=9 CLASS=\"TableDarkLabel\"><IMG SRC=\"darkcorner.jpg\" align=top></TD>");
            sw.WriteLine("<TD CLASS=\"TableDarkLabel\" WIDTH=206>Directory</TD>");
            sw.WriteLine("<TD CLASS=\"TableDarkLabel\" >Created</TD>");
            sw.WriteLine("<TD CLASS=\"TableDarkLabel\" >Last Write</TD>");
            sw.WriteLine("<TD CLASS=\"TableDarkLabel\" >Last Accessed</TD>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("</TR>");
            foreach (string str in dirs)
            {
                di = new DirectoryInfo(str);

                modDir = di.FullName.Replace(prop.Default.DirectoryToSearch, "&ltroot&gt");
                compressedDirectory = GetCompressedFileName(di.FullName.Replace(prop.Default.DirectoryToSearch, ""));

                sw.WriteLine("<TR height=20>");
                sw.WriteLine("<TD width=20>&nbsp;</TD>");
                sw.WriteLine("<TD CLASS=\"TableDarkDesc\">&nbsp;</TD>");
                sw.WriteLine("<TD CLASS=\"TableDarkDesc\"><A HREF=\"" + compressedDirectory + "/" + compressedDirectory + ".HTM\" TARGET=\"_top\">" + modDir + "</A></TD>");
                sw.WriteLine("<TD CLASS=\"TableDarkDesc\">" + di.CreationTime + "</TD>");
                sw.WriteLine("<TD CLASS=\"TableDarkDesc\">" + di.LastWriteTime + "</TD>");
                sw.WriteLine("<TD CLASS=\"TableDarkDesc\">" + di.LastAccessTime + "</TD>");
                sw.WriteLine("<TD width=20>&nbsp;</TD>");
                sw.WriteLine("</TR>");
            }
            sw.WriteLine("</TABLE>");
            sw.WriteLine("</BODY>");
            sw.WriteLine("</HTML>");
            sw.Close();
        }

        private void CreateDirectory()
        {
            DirectoryInfo di = new DirectoryInfo(Directory);
            if (!di.Exists)
            {
                try
                {

                    di.Create();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to create directory.");
                    MessageBox.Show("Exception seen was: " + ex.ToString());
                }
            }
            else
            {
                DialogResult dr = MessageBox.Show("Directory already exists. Do you want to delete the existing directory?", "Directory Exists", MessageBoxButtons.YesNo);
                if (dr == DialogResult.Yes)
                    try
                    {
                        di.Delete(true);
                        di.Create();

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to delete directory.");
                        MessageBox.Show("Exception seen was: " + ex.ToString());
                        Application.Exit();
                    }
            }
        }

        private void RenderProgramHeaderPage(string parentDir, Header hdr)
        {
            StreamWriter sw = new StreamWriter(parentDir + @"\programHeader.htm");
            Regex regex = new Regex("\r\n");
            string[] code = regex.Split(hdr.Code);
            FileInfo fi = new FileInfo(hdr.ProgramFile);

            sw.WriteLine("<HTML>");
            sw.WriteLine("<HEAD>");
            sw.WriteLine("<META http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
            sw.WriteLine("<TITLE>" + hdr.Remarks + "</TITLE>");
            sw.WriteLine("<LINK REL=STYLESHEET HREF=\"../../CommentReport.css\" TYPE=\"text/css\">");
            sw.WriteLine("</HEAD>");
            sw.WriteLine("<BODY topmargin=0 rightmargin=0 leftmargin=0 style=\"background-image: url(../titletile.jpg); background-repeat:repeat-x; background-position: 0 0;\" >");
            sw.WriteLine("<DIV CLASS=\"PageHeading\">" + fi.Name + ",");
            sw.WriteLine("<SPAN Class=\"SmallItalics\">Copyright " + hdr.Company + ", " + hdr.CopyRightYear + ", All rights reserved.</SPAN>");
            sw.WriteLine("</DIV>");
            sw.WriteLine("<DIV CLASS=\"Description\">");
            sw.WriteLine("" + hdr.Name + "");
            sw.WriteLine("<P>");
            sw.WriteLine("");
            sw.WriteLine("</DIV>");
            sw.WriteLine("<DIV CLASS=\"Description\">");
            sw.WriteLine("" + hdr.Description + "");
            sw.WriteLine("<P>");
            sw.WriteLine("");
            sw.WriteLine("</DIV>");
            sw.WriteLine("<IMG src=\"../../GradLeft.jpg\" width=7 height=378 alt=\"\" border=\"0\" style=\"position:absolute; left:10; top:18;z-Index:2\">");
            sw.WriteLine("<IMG src=\"../../GradTop.jpg\" width=352 height=7 alt=\"\" border=\"0\" align=\"top\" style=\"position:absolute; left:10; top:18; z-index:1\">");
            sw.WriteLine("<DIV CLASS=\"Remarks\">");
            sw.WriteLine("<SPAN CLASS=\"RemarkHdrX\">Author: </SPAN>" + hdr.Author + "</DIV>");
            sw.WriteLine("<DIV CLASS=\"Remarks\">");
            sw.WriteLine("<SPAN CLASS=\"RemarkHdrX\">Usage: </SPAN>" + hdr.Usage + "");
            sw.WriteLine("</DIV>");
            sw.WriteLine("<DIV CLASS=\"Remarks\">");
            sw.WriteLine("<SPAN CLASS=\"RemarkHdrX\">Remarks: </SPAN>" + hdr.Remarks + "");
            sw.WriteLine("</DIV>");
            sw.WriteLine("<TABLE CLASS=\"InfoTable\" cellpadding=0 cellspacing=0>");
            sw.WriteLine("<TR height=20>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("<TD valign=top align=left width=9 CLASS=\"TableDarkLabel\"><IMG SRC=\"../../darkcorner.jpg\" align=top></TD>");
            sw.WriteLine("<TD CLASS=\"TableDarkLabel\" WIDTH=100 >Initials</TD>");
            sw.WriteLine("<TD CLASS=\"TableDarkLabel\" WIDTH=100 >Date</TD>");
            sw.WriteLine("<TD CLASS=\"TableDarkLabel\">Action</TD>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("</TR>");

            foreach (SaviDocAction action in hdr.SaviDocActions)
            {

                sw.WriteLine("<TR height=20>");
                sw.WriteLine("<TD width=20>&nbsp;</TD>");
                sw.WriteLine("<TD CLASS=\"TableDarkDesc\">&nbsp;</TD>");
                sw.WriteLine("<TD CLASS=\"TableDarkDesc\" width=100 >" + action.Initials + "</TD>");
                sw.WriteLine("<TD CLASS=\"TableDarkDesc\" width=100 >" + action.Date + "</TD>");
                sw.WriteLine("<TD CLASS=\"TableDarkDesc\">" + action.ActionTaken + "</TD>");
                sw.WriteLine("<TD width=20>&nbsp;</TD>");
                sw.WriteLine("</TR>");
            }

            sw.WriteLine("<TABLE CLASS=\"InfoTable\" cellpadding=0 cellspacing=0>");
            sw.WriteLine("<TR height=20>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("<TD valign=top align=left width=9 bgcolor=\"#6699ff\"><IMG SRC=\"../../bluecorner.jpg\" align=top></TD>");
            sw.WriteLine("<TD CLASS=\"CodeLightLabel\">Code</TD>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("</TR>");
            sw.WriteLine("<TR height=20>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("<TD CLASS=\"CodeLightDesc\">&nbsp;</TD>");
            sw.WriteLine("<TD CLASS=\"CodeLightDesc\">");
            sw.WriteLine("<pre>");
            foreach (string str in code)
            {
                sw.WriteLine(System.Web.HttpUtility.HtmlEncode(new Regex(@"\t").Replace(str, "    ")));
            }

            sw.WriteLine("</pre> ");
            sw.WriteLine("	</TD>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("</TR>");
            sw.WriteLine("</TABLE>");
            sw.WriteLine("</BODY>");
            sw.WriteLine("</HTML>");
            sw.Close();
        }

        private void RenderIndividualCommentPages(string parentDir, CodeComment cc)
        {
            Regex regex = new Regex("\r\n");
            string[] code = regex.Split(cc.Code);

            StreamWriter sw = new StreamWriter(parentDir + @"\" + cc.LinkFile);
            sw.WriteLine("<HTML>");
            sw.WriteLine("<HEAD>");
            sw.WriteLine("<META http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">");
            sw.WriteLine("<TITLE>" + cc.Name + "</TITLE>");
            sw.WriteLine("<LINK REL=STYLESHEET HREF=\"../../CommentReport.css\" TYPE=\"text/css\">");
            sw.WriteLine("</HEAD>");
            sw.WriteLine("<BODY topmargin=0 rightmargin=0 leftmargin=0 style=\"background-image: url(../titletile.jpg); background-repeat:repeat-x; background-position: 0 0;\" >");
            sw.WriteLine("<DIV CLASS=\"PageHeading\">" + cc.TypeOfCode + ": " + cc.Name + "</DIV>");
            sw.WriteLine("<DIV CLASS=\"Description\">");
            sw.WriteLine(cc.Remarks);
            sw.WriteLine("<P>");
            sw.WriteLine("");
            sw.WriteLine("</DIV>");
            sw.WriteLine("<IMG src=\"../../GradLeft.jpg\" width=7 height=378 alt=\"\" border=\"0\" style=\"position:absolute; left:10; top:18;z-Index:2\">");
            sw.WriteLine("<IMG src=\"../../GradTop.jpg\" width=352 height=7 alt=\"\" border=\"0\" align=\"top\" style=\"position:absolute; left:10; top:18; z-index:1\">");
            sw.WriteLine("<DIV CLASS=\"Remarks\">");
            sw.WriteLine("<SPAN CLASS=\"RemarkHdrX\">Returns: </SPAN>" + cc.Returns + "</DIV>");
            sw.WriteLine("<DIV CLASS=\"Remarks\">");
            sw.WriteLine("<SPAN CLASS=\"RemarkHdrX\">Example: </SPAN>" + cc.Example + "</DIV>");
            sw.WriteLine("<DIV CLASS=\"Remarks\">");
            sw.WriteLine("<SPAN CLASS=\"RemarkHdrX\">See Also: </SPAN><a href=\"" + cc.SeeAlso + "\" target=\"_blank\">" + cc.SeeAlso + "</a></DIV>");

            if (cc.Parameters.Count > 0)
            {

                sw.WriteLine("<TABLE CLASS=\"InfoTable\" cellpadding=0 cellspacing=0>");
                sw.WriteLine("<TR height=20>");
                sw.WriteLine("<TD width=20>&nbsp;</TD>");
                sw.WriteLine("<TD valign=top align=left width=9 bgcolor=\"#cccc66\"><IMG SRC=\"../../graycorner.jpg\" align=top></TD>");
                sw.WriteLine("<TD CLASS=\"TableLightLabel\" WIDTH=150>Parameter</TD>");
                sw.WriteLine("<TD CLASS=\"TableLightLabel\" WIDTH=110>Default</TD>");
                sw.WriteLine("<TD CLASS=\"TableLightLabel\" >Description</TD>");
                sw.WriteLine("<TD width=20>&nbsp;</TD>");
                sw.WriteLine("</TR>");

                foreach (Parameter parm in cc.Parameters)
                {

                    sw.WriteLine("<TR height=20>");
                    sw.WriteLine("<TD width=20>&nbsp;</TD>");
                    sw.WriteLine("<TD CLASS=\"TableLightDesc\">&nbsp;</TD>");
                    sw.WriteLine("<TD CLASS=\"TableLightDesc\">" + parm.Name + "</TD>");
                    sw.WriteLine("<TD CLASS=\"TableLightDesc\">" + parm.DefaultValue + "</TD>");
                    sw.WriteLine("<TD CLASS=\"TableLightDesc\">" + parm.Remarks + "</TD>");
                    sw.WriteLine("<TD width=20>&nbsp;</TD>");
                    sw.WriteLine("</TR>");
                }
                sw.WriteLine("</TABLE>");
            }

            if (cc.Inputs.Count > 0)
            {

                sw.WriteLine("<TABLE CLASS=\"InfoTable\" cellpadding=0 cellspacing=0>");
                sw.WriteLine("<TR height=20>");
                sw.WriteLine("<TD width=20>&nbsp;</TD>");
                sw.WriteLine("<TD valign=top align=left width=9 bgcolor=\"#cccc66\"><IMG SRC=\"../../graycorner.jpg\" align=top></TD>");
                sw.WriteLine("<TD CLASS=\"TableLightLabel\" WIDTH=150>Input</TD>");
                sw.WriteLine("<TD CLASS=\"TableLightLabel\" >Description</TD>");
                sw.WriteLine("<TD width=20>&nbsp;</TD>");
                sw.WriteLine("</TR>");

                foreach (InOutParm inOut in cc.Inputs)
                {
                    sw.WriteLine("<TR height=20>");
                    sw.WriteLine("<TD width=20>&nbsp;</TD>");
                    sw.WriteLine("<TD CLASS=\"TableLightDesc\">&nbsp;</TD>");
                    sw.WriteLine("<TD CLASS=\"TableLightDesc\">" + inOut.Name + "</TD>");
                    sw.WriteLine("<TD CLASS=\"TableLightDesc\">" + inOut.Remarks + "</TD>");
                    sw.WriteLine("<TD width=20>&nbsp;</TD>");
                    sw.WriteLine("</TR>");
                }
                sw.WriteLine("</TABLE>");
            }

            if (cc.Outputs.Count > 0)
            {
                sw.WriteLine("<TABLE CLASS=\"InfoTable\" cellpadding=0 cellspacing=0>");
                sw.WriteLine("<TR height=20>");
                sw.WriteLine("<TD width=20>&nbsp;</TD>");
                sw.WriteLine("<TD valign=top align=left width=9 bgcolor=\"#cccc66\"><IMG SRC=\"../../graycorner.jpg\" align=top></TD>");
                sw.WriteLine("<TD CLASS=\"TableLightLabel\" WIDTH=150>Output</TD>");
                sw.WriteLine("<TD CLASS=\"TableLightLabel\" >Description</TD>");
                sw.WriteLine("<TD width=20>&nbsp;</TD>");
                sw.WriteLine("</TR>");

                foreach (InOutParm inOut in cc.Outputs)
                {
                    sw.WriteLine("<TR height=20>");
                    sw.WriteLine("<TD width=20>&nbsp;</TD>");
                    sw.WriteLine("<TD CLASS=\"TableLightDesc\">&nbsp;</TD>");
                    sw.WriteLine("<TD CLASS=\"TableLightDesc\">" + inOut.Name + "</TD>");
                    sw.WriteLine("<TD CLASS=\"TableLightDesc\">" + inOut.Remarks + "</TD>");
                    sw.WriteLine("<TD width=20>&nbsp;</TD>");
                    sw.WriteLine("</TR>");
                }
                sw.WriteLine("</TABLE>");
            }

            sw.WriteLine("<TABLE CLASS=\"InfoTable\" cellpadding=0 cellspacing=0>");
            sw.WriteLine("<TR height=20>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("<TD valign=top align=left width=9 bgcolor=\"#6699ff\"><IMG SRC=\"../../bluecorner.jpg\" align=top></TD>");
            sw.WriteLine("<TD CLASS=\"CodeLightLabel\">Code</TD>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("</TR>");
            sw.WriteLine("<TR height=20>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("<TD CLASS=\"CodeLightDesc\">&nbsp;</TD>");
            sw.WriteLine("<TD CLASS=\"CodeLightDesc\">");
            sw.WriteLine("<pre>");

            foreach (string str in code)
            {
                sw.WriteLine(System.Web.HttpUtility.HtmlEncode(new Regex(@"\t").Replace(str, "    ")));
            }

            sw.WriteLine("</pre> ");
            sw.WriteLine("	</TD>");
            sw.WriteLine("<TD width=20>&nbsp;</TD>");
            sw.WriteLine("</TR>");
            sw.WriteLine("</TABLE>");
            sw.WriteLine("</BODY>");
            sw.WriteLine("</HTML>");
            sw.Close();
        }
    }
}
