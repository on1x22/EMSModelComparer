using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Monitel.Mal;
using Monitel.Mal.Context.CIM16.Ext.EMS;
using Monitel.CIM.Aggregation;
using Monitel.Protocol.Common;
using Monitel.UI.Infrastructure.Services;
using Monitel.Mal.Context.CIM16;
using Monitel.DataContext.Tools.ModelExtensions;

namespace EMSModelComparer
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        public static void Main() 
        {
			try
			{
				FolderWithAppFilesHandler programFolder = new FolderWithAppFilesHandler();
				programFolder.CreateFolderWithAppFilesIfAbsent();

				FileHandler fileHandler = new FileHandler(programFolder);
				fileHandler.CreateAppFiles();
				fileHandler.ReadDataFromConfigFile();

				MainWindow mainWindow = new MainWindow(fileHandler, programFolder, MainWindow, Services);
				mainWindow.InitializeWindow();

				mainWindow.Start();
			}
			catch (Exception ex) { MessageBox.Show(ex.Message); }
		}
    }
}
