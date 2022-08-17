using System;


namespace EMSModelComparer
{
	internal class FolderWithAppFilesHandler
	{
		readonly string pathToScriptFiles;
		public string PathToScriptFiles
		{
			get
			{
				return pathToScriptFiles;
			}
			/*set
            {
				pathToScriptFiles = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EMS model comparer";
			}*/
		}

		internal FolderWithAppFilesHandler()
		{
			pathToScriptFiles = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EMS model comparer";
		}

		internal void CreateFolderWithAppFilesIfAbsent()
		{
			if (System.IO.Directory.Exists(pathToScriptFiles) == false)
			{
				System.IO.Directory.CreateDirectory(pathToScriptFiles);
			}
		}
	}
}
