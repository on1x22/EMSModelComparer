using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMSModelComparer
{
    internal class Logger
    {
        private FolderWithAppFilesHandler programFolder;

        internal Logger(FolderWithAppFilesHandler folderWithAppFiles)
        {
            programFolder = folderWithAppFiles;
        }

        internal void Write(Severity severity, string message)
        {
            string filePath = programFolder.PathToScriptFiles + @"\EMSMCLog.txt";            

            using (StreamWriter sw = new StreamWriter(filePath, true, Encoding.Default))
            {
                DateTime dtNow = DateTime.Now;

                sw.WriteLine(dtNow.ToString(new CultureInfo("ru-RU")) + "  [" + severity.ToString() + "]  " + message);
            }
        }
    }

    internal enum Severity
    {
        Info,
        Error       
    }
}
