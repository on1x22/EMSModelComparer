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
				/*FolderWithAppFilesHandler programFolder = new FolderWithAppFilesHandler();
				programFolder.CreateFolderWithAppFilesIfAbsent();

				FileHandler fileHandler = new FileHandler(programFolder);
				fileHandler.CreateAppFiles();
				fileHandler.ReadDataFromConfigFile();

				WorkWindow workWindow = new WorkWindow(fileHandler, programFolder, MainWindow, Services);
				workWindow.InitializeWindow();

				workWindow.Start();*/

				IModel model = new EMSModel();
				IController controller = new EMSController(MainWindow, Services, model);

			}
			catch (Exception ex) { MessageBox.Show(ex.Message); }
		}
    }
}
