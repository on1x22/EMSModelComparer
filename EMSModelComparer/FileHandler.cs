using System;


namespace EMSModelComparer
{
    internal class FileHandler
    {
		private readonly string configFilePath;
		private string odbServerName = String.Empty;
		private string reverseOdbInstanseName = String.Empty;
		private string reverseOdbModelVersionId = String.Empty;
		private string forwardOdbInstanseName = String.Empty;
		private string forwardOdbModelVersionId = String.Empty;
		private readonly FolderWithAppFilesHandler programFolder;

		public string ConfigFilePath
		{
			get
			{
				return configFilePath;
			}
		}

		public string OdbServerName
		{
			get
			{
				return odbServerName;
			}
			set
			{
				odbServerName = value;
			}
		}

		public string ReverseOdbInstanseName
		{
			get
			{
				return reverseOdbInstanseName;
			}
			set
			{
				reverseOdbInstanseName = value;
			}
		}

		public string ReverseOdbModelVersionId
		{
			get
			{
				return reverseOdbModelVersionId;
			}
			set
			{
				reverseOdbModelVersionId = value;
			}
		}

		public string ForwardOdbInstanseName
		{
			get
			{
				return forwardOdbInstanseName;
			}
			set
			{
				forwardOdbInstanseName = value;
			}
		}

		public string ForwardOdbModelVersionId
		{
			get
			{
				return forwardOdbModelVersionId;
			}
			set
			{
				forwardOdbModelVersionId = value;
			}
		}



		internal FileHandler(FolderWithAppFilesHandler programFolder)
		{
			this.programFolder = programFolder;
			programFolder.CreateFolderWithAppFilesIfAbsent();
			configFilePath = programFolder.PathToScriptFiles + @"\EMSMCconfig.csv";
		}

		internal void ReadDataFromConfigFile()
		{
			System.IO.FileInfo fileInfo = new System.IO.FileInfo(configFilePath);
			if (fileInfo.Exists)
			{
				System.IO.StreamReader srConfig = new System.IO.StreamReader(configFilePath, System.Text.Encoding.Default, false);

				string line;
				while ((line = srConfig.ReadLine()) != null)
				{
					string[] scriptParams = line.Split(new char[] { ';' });
					switch (scriptParams[0])
					{
						case "OdbServerName":
							odbServerName = scriptParams[1];
							break;
						case "ReverseOdbInstanseName":
							reverseOdbInstanseName = scriptParams[1];
							break;
						case "ReverseOdbModelVersionId":
							reverseOdbModelVersionId = scriptParams[1];
							break;
						case "ForwardOdbInstanseName":
							forwardOdbInstanseName = scriptParams[1];
							break;
						case "ForwardOdbModelVersionId":
							forwardOdbModelVersionId = scriptParams[1];
							break;
						default:
							throw new Exception("Некорректный формат файла");
					}
				}

				srConfig.Close();
			}
			else
			{
				throw new NullReferenceException();
			}
		}

		internal void CreateAppFiles()
		{
			System.IO.FileInfo fileInfo = new System.IO.FileInfo(programFolder.PathToScriptFiles + @"\EMSMCconfig.csv");
			if (!fileInfo.Exists)
				fileInfo.Create();

			fileInfo = new System.IO.FileInfo(programFolder.PathToScriptFiles + @"\ExceptionPropertyList.csv");
			if (!fileInfo.Exists)
				fileInfo.Create();

			fileInfo = new System.IO.FileInfo(programFolder.PathToScriptFiles + @"\ReverseHash.csv");
			if (!fileInfo.Exists)
				fileInfo.Create();

			fileInfo = new System.IO.FileInfo(programFolder.PathToScriptFiles + @"\ForwardHash.csv");
			if (!fileInfo.Exists)
				fileInfo.Create();

			fileInfo = new System.IO.FileInfo(programFolder.PathToScriptFiles + @"\Перечень изменений.csv");
			if (!fileInfo.Exists)
				fileInfo.Create();
		}
	}
}
