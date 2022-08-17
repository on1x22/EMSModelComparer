using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using Monitel.Mal;
using Monitel.Mal.Context.CIM16.Ext.EMS;
using Monitel.CIM.Aggregation;
using Monitel.Protocol.Common;
//using Monitel.CIM.Ext.EMS;
//using Monitel.DataContext.Tools.ModelExtensions;
//using Monitel.PlatformInfrastructure;
//using Monitel.Supervisor.Infrastructure;
using Monitel.UI.Infrastructure.Services;
using Monitel.Mal.Context.CIM16;
using Monitel.DataContext.Tools.ModelExtensions;

namespace EMSModelComparer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

			/// v0.1. (21.02.2022) Начальная
			///
			/// При первоначальном открытии программы в папке "Документы" создается папка "EMS model comparer", а также файлы, необходимые для работы программы.
			///	Наименование сервера задается в формате: *** (название можно узнать в Менеджере версий модели по пути Вид\Информация о модели\Сервер).
			/// Наименование БД задается в формате: ODB_EnergyMain (название можно узнать в Менеджере версий модели по пути Вид\Информация о модели\Имя базы данных).
			/// При запуске выполнения скрипта информация о сравниваемых моделях записывается в файл EMSMCconfig.csv. При последующих запусках программы сохраненные данные
			/// автоматически заполняют форму.
			///
			/// Алгоритм работы программы:
			/// 1. В заданных моделях выполняется поиск объектов по указанной роли (ДЦ + тип роли), а также всех дочерних и связанных объектов ИМ, необходимых для работы 
			///    соответствующей подзадачи СК-11 (МТН, КПОС, МУН). Найденные объекты хранятся в памяти программы и записываются в файлы ReverseHash.csv и ForwardHash.csv (для отладки);
			/// 2. Выполняется проход по каждому свойству, каждого объекта исходной модели и производится сравнение с аналогичными свойствами объекта в сравниваемой модели. Результаты 
			///    сравнения моделей записываются в файл Перечень изменений.csv;
			/// 3. При наличии значений свойств в файле ExceptionPropertyList.csv эти свойства игнорируются у всех сравниваемых объектов и в итоговый файл с резултатами сравнениями не 
			///    попадают.
			/// 4. Замечания/ошибки/предложения по работе программы прошу присыласть на почту alehinra@odusv.so-ups.ru.

			try
			{
				FolderWithAppFilesHandler programFolder = new FolderWithAppFilesHandler();
				programFolder.CreateFolderWithAppFilesIfAbsent();


				/*// Путь к файлам скрипта
				var pathToScriptFiles = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EMS model comparer";

				string md = Environment.GetFolderPath(Environment.SpecialFolder.Personal);//путь к Документам
				if (System.IO.Directory.Exists(pathToScriptFiles) == false)
				{
					System.IO.Directory.CreateDirectory(pathToScriptFiles);
				}*/

				FileHandler fileHandler = new FileHandler(programFolder);
				fileHandler.CreateAppFiles();
				fileHandler.ReadDataFromConfigFile();

				/*// Проверка наличия и чтение настроек скрипта
				var _odbServerName = String.Empty;
				var reverseOdbInstanseName = String.Empty;
				var reverseOdbModelVersionId = String.Empty;
				var forwardOdbInstanseName = String.Empty;
				var forwardOdbModelVersionId = String.Empty;

				string configFilePath = programFolder.PathToScriptFiles + @"\EMSMCconfig.csv";
				System.IO.FileInfo fileInf = new System.IO.FileInfo(configFilePath);
				if (fileInf.Exists)
				{
					System.IO.StreamReader srConfig = new System.IO.StreamReader(configFilePath, System.Text.Encoding.Default, false);

					string line;
					while ((line = srConfig.ReadLine()) != null)
					{
						string[] scriptParams = line.Split(new char[] { ';' });
						if (scriptParams[0] == "OdbServerName")
							_odbServerName = scriptParams[1];						
						if (scriptParams[0] == "ReverseOdbInstanseName")
							reverseOdbInstanseName = scriptParams[1];
						if (scriptParams[0] == "ReverseOdbModelVersionId")
							reverseOdbModelVersionId = scriptParams[1];
						if (scriptParams[0] == "ForwardOdbInstanseName")
							forwardOdbInstanseName = scriptParams[1];
						if (scriptParams[0] == "ForwardOdbModelVersionId")
							forwardOdbModelVersionId = scriptParams[1];
					}

					srConfig.Close();
				}
				else
				{
					fileInf.Create();
				}

				// Проверка наличия необходимых для скрипта файлов
				fileInf = new System.IO.FileInfo(programFolder.PathToScriptFiles + @"\ExceptionPropertyList.csv");
				if (!fileInf.Exists)
					fileInf.Create();
				fileInf = new System.IO.FileInfo(programFolder.PathToScriptFiles + @"\ReverseHash.csv");
				if (!fileInf.Exists)
					fileInf.Create();
				fileInf = new System.IO.FileInfo(programFolder.PathToScriptFiles + @"\ForwardHash.csv");
				if (!fileInf.Exists)
					fileInf.Create();
				fileInf = new System.IO.FileInfo(programFolder.PathToScriptFiles + @"\Перечень изменений.csv");
				if (!fileInf.Exists)
					fileInf.Create();*/

				// Потоки для работы с файлами		
				System.IO.StreamWriter swReverse = null;
				System.IO.StreamWriter swForward = null;
				System.IO.StreamWriter swDiff = null;
				System.IO.StreamReader srExcep = null;

				// Список с измененияим в модели
				List<string> listOfDifferences = new List<string>();
				HashSet<string> exceptionPropertyHash = null;

				ModelImage reverseModelImage;
				ModelImage forwardModelImage;

				//Создание главного окна, scroll и StackPanel
				Window mainWindow = new Window();
				var scroll = new System.Windows.Controls.ScrollViewer();
				var mainStackPanel = new System.Windows.Controls.StackPanel();

				//Инициализация главного окна
				mainWindow.Title = "Окно замечаний";
				mainWindow.Width = 750;
				mainWindow.MaxWidth = 750;
				mainWindow.Content = scroll;
				mainWindow.ResizeMode = ResizeMode.NoResize;
				mainWindow.SizeToContent = SizeToContent.Height;

				//Инициализация элемента scroll
				scroll.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Visible;
				scroll.Content = mainStackPanel;

				//Обработчик события при прокрутке scroll
				Action<object, System.Windows.RoutedEventArgs> ScrollDown = (sender, e) =>
				{
					scroll.LineDown();
				};
				scroll.ScrollChanged += new System.Windows.Controls.ScrollChangedEventHandler(ScrollDown);

				Action<object, System.Windows.RoutedEventArgs> ScrollUp = (sender, e) =>
				{
					scroll.LineUp();
				};
				scroll.ScrollChanged += new System.Windows.Controls.ScrollChangedEventHandler(ScrollUp);

				var expander = new System.Windows.Controls.Expander();

				//=======МОЁ (НАЧАЛО)===================================================================

				// GroupBox для вывода организации и роли
				var grBoxOrganisation = new System.Windows.Controls.GroupBox();
				grBoxOrganisation.IsEnabled = true;
				grBoxOrganisation.Header = "Настройки организации и роли";
				grBoxOrganisation.Margin = new Thickness(5, 5, 5, 5);

				var orgStackPanel = new System.Windows.Controls.StackPanel();

				// Grid для параметров организации
				var organisationGrid = new System.Windows.Controls.Grid();
				organisationGrid.ShowGridLines = false;
				organisationGrid.Margin = new Thickness(5, 5, 5, 0);
				organisationGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
				organisationGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
				organisationGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});
				organisationGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});

				// TextBlock для ввода проверяемой организации
				var textBlockOrganisation = new System.Windows.Controls.TextBlock();
				textBlockOrganisation.IsEnabled = true;
				textBlockOrganisation.Text = "Организация (UID): ";

				// TextBox для ввода проверяемой организации
				var textBoxOrganisation = new System.Windows.Controls.TextBox();
				textBoxOrganisation.IsEnabled = true;
				textBoxOrganisation.Margin = new Thickness(5, 5, 5, 5);

				// Grid для параметров роли
				var roleGrid = new System.Windows.Controls.Grid();
				roleGrid.ShowGridLines = false;
				roleGrid.Margin = new Thickness(5, 5, 5, 0);
				roleGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
				roleGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
				roleGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});
				roleGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});

				// TextBlock для ввода проверяемой роли организации
				var textBlockOrganisationRole = new System.Windows.Controls.TextBlock();
				textBlockOrganisationRole.IsEnabled = true;
				textBlockOrganisationRole.Text = "Роль организации (UID): ";

				var ComboBoxOrgRole = new System.Windows.Controls.ComboBox();
				ComboBoxOrgRole.IsEnabled = true;
				ComboBoxOrgRole.Margin = new Thickness(5, 5, 5, 5);
				ComboBoxOrgRole.Items.Add("Контроль в МТН");
				ComboBoxOrgRole.Items.Add("Контроль в КПОС (в планах)");
				ComboBoxOrgRole.Items.Add("Контроль в МУН (в планах)");
				ComboBoxOrgRole.SelectedIndex = 0;

				mainStackPanel.Children.Add(grBoxOrganisation);

				// Включение элементов в organisationGrid
				System.Windows.Controls.Grid.SetRow(textBlockOrganisation, 0);
				System.Windows.Controls.Grid.SetColumn(textBlockOrganisation, 0);

				System.Windows.Controls.Grid.SetRow(textBoxOrganisation, 0);
				System.Windows.Controls.Grid.SetColumn(textBoxOrganisation, 1);

				System.Windows.Controls.Grid.SetRow(textBlockOrganisationRole, 0);
				System.Windows.Controls.Grid.SetColumn(textBlockOrganisationRole, 0);

				System.Windows.Controls.Grid.SetRow(ComboBoxOrgRole, 0);
				System.Windows.Controls.Grid.SetColumn(ComboBoxOrgRole, 1);

				organisationGrid.Children.Add(textBlockOrganisation);
				organisationGrid.Children.Add(textBoxOrganisation);
				roleGrid.Children.Add(textBlockOrganisationRole);
				roleGrid.Children.Add(ComboBoxOrgRole);

				orgStackPanel.Children.Add(organisationGrid);
				orgStackPanel.Children.Add(roleGrid);

				grBoxOrganisation.Content = orgStackPanel;



				// GroupBox для настройки директории используемых файлов
				var grBoxFilesDir = new System.Windows.Controls.GroupBox();
				grBoxFilesDir.IsEnabled = true;
				grBoxFilesDir.Header = "Настройки папки с файлами";
				grBoxFilesDir.Margin = new Thickness(5, 5, 5, 5);

				var stackPanelFilesDir = new System.Windows.Controls.StackPanel();

				// Grid для параметров папки
				var fileDirGrid = new System.Windows.Controls.Grid();
				fileDirGrid.ShowGridLines = false;
				fileDirGrid.Margin = new Thickness(5, 5, 5, 0);
				fileDirGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
				fileDirGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
				fileDirGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});
				fileDirGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});

				// TextBlock для ввода папки для работы с файлами
				var textBlockFilesDir = new System.Windows.Controls.TextBlock();
				textBlockFilesDir.IsEnabled = true;
				textBlockFilesDir.Text = "Папка для работы с файлами: ";

				// TextBox для ввода папки для работы с файлами
				var textBoxDirPath = new System.Windows.Controls.TextBox();
				textBoxDirPath.IsEnabled = true;
				textBoxDirPath.MinWidth = 200;
				textBoxDirPath.Margin = new Thickness(5, 5, 5, 5);

				// TextBlock для ввода комментария по папке
				var textBlockFilesDirComment = new System.Windows.Controls.TextBlock();
				textBlockFilesDirComment.IsEnabled = true;
				textBlockFilesDirComment.Text = "В указанной папке будут создаваться или перезаписываться файлы \nExceptionPropertyList.csv, ForwardHash.csv, ReverseHash.csv, \nПеречень изменений по МТН.csv";

				//mainStackPanel.Children.Add(grBoxFilesDir);

				System.Windows.Controls.Grid.SetRow(textBlockFilesDir, 0);
				System.Windows.Controls.Grid.SetColumn(textBlockFilesDir, 0);

				System.Windows.Controls.Grid.SetRow(textBoxDirPath, 0);
				System.Windows.Controls.Grid.SetColumn(textBoxDirPath, 1);

				fileDirGrid.Children.Add(textBlockFilesDir);
				fileDirGrid.Children.Add(textBoxDirPath);

				stackPanelFilesDir.Children.Add(fileDirGrid);
				stackPanelFilesDir.Children.Add(textBlockFilesDirComment);

				grBoxFilesDir.Content = stackPanelFilesDir;


				// GroupBox для выбора сравниваемых моделей
				var grBoxComparedModels = new System.Windows.Controls.GroupBox();
				grBoxComparedModels.IsEnabled = true;
				grBoxComparedModels.Header = "Настройки сравниваемых моделей";
				grBoxComparedModels.Margin = new Thickness(5, 5, 5, 5);

				var stackPanelComparedModels = new System.Windows.Controls.StackPanel();

				// Grid для настроки сервера
				var serverGrid = new System.Windows.Controls.Grid();
				serverGrid.ShowGridLines = false;
				serverGrid.Margin = new Thickness(5, 5, 5, 0);
				serverGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
				serverGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
				serverGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});
				serverGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});

				// TextBlock для ввода контекста исходной модели
				var textBlockServerName = new System.Windows.Controls.TextBlock();
				textBlockServerName.IsEnabled = true;
				textBlockServerName.Text = "Имя сервера: ";

				// TextBox для ввода контекста исходной модели
				var textBoxServerName = new System.Windows.Controls.TextBox();
				textBoxServerName.IsEnabled = true;
				textBoxServerName.Width = 200;
				textBoxServerName.Margin = new Thickness(5, 5, 5, 5);
				textBoxServerName.Text = fileHandler.OdbServerName/*_odbServerName*/;

				// Grid для настроки контекста исходной модели
				var reverseModelContextGrid = new System.Windows.Controls.Grid();
				reverseModelContextGrid.ShowGridLines = false;
				reverseModelContextGrid.Margin = new Thickness(5, 5, 5, 0);
				reverseModelContextGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
				reverseModelContextGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
				reverseModelContextGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});
				reverseModelContextGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});

				// TextBlock для ввода контекста исходной модели
				var textBlockReverseModelContext = new System.Windows.Controls.TextBlock();
				textBlockReverseModelContext.IsEnabled = true;
				textBlockReverseModelContext.Text = "Контекст исходной модели: ";

				// TextBox для ввода контекста исходной модели
				var textBoxReverseModelContext = new System.Windows.Controls.TextBox();
				textBoxReverseModelContext.IsEnabled = true;
				textBoxReverseModelContext.Width = 200;
				textBoxReverseModelContext.Margin = new Thickness(5, 5, 5, 5);
				textBoxReverseModelContext.Text = fileHandler.ReverseOdbInstanseName /*reverseOdbInstanseName*/;

				// Grid для настроки номера исходной модели
				var reverseModelNumGrid = new System.Windows.Controls.Grid();
				reverseModelNumGrid.ShowGridLines = false;
				reverseModelNumGrid.Margin = new Thickness(5, 5, 5, 0);
				reverseModelNumGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
				reverseModelNumGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
				reverseModelNumGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});
				reverseModelNumGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});

				// TextBlock для ввода номера исходной модели
				var textBlockReverseModelNum = new System.Windows.Controls.TextBlock();
				textBlockReverseModelNum.IsEnabled = true;
				textBlockReverseModelNum.Text = "Номер исходной модели: ";

				// TextBox для ввода номера исходной модели
				var textBoxReverseModelNum = new System.Windows.Controls.TextBox();
				textBoxReverseModelNum.IsEnabled = true;
				textBoxReverseModelNum.Width = 200;
				textBoxReverseModelNum.Margin = new Thickness(5, 5, 5, 5);
				textBoxReverseModelNum.Text = fileHandler.ReverseOdbModelVersionId/*reverseOdbModelVersionId*/;

				// Grid для настроки контекста сравниваемой модели
				var forwardModelContextGrid = new System.Windows.Controls.Grid();
				forwardModelContextGrid.ShowGridLines = false;
				forwardModelContextGrid.Margin = new Thickness(5, 5, 5, 0);
				forwardModelContextGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
				forwardModelContextGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
				forwardModelContextGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});
				forwardModelContextGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});

				// TextBlock для ввода контекста сравниваемой модели
				var textBlockForwardModelContext = new System.Windows.Controls.TextBlock();
				textBlockForwardModelContext.IsEnabled = true;
				textBlockForwardModelContext.Text = "Контекст сравниваемой модели: ";

				// TextBox для ввода контекста сравниваемой модели
				var textBoxForwardModelContext = new System.Windows.Controls.TextBox();
				textBoxForwardModelContext.IsEnabled = true;
				textBoxForwardModelContext.Width = 200;
				textBoxForwardModelContext.Margin = new Thickness(5, 5, 5, 5);
				textBoxForwardModelContext.Text = fileHandler.ForwardOdbInstanseName/*forwardOdbInstanseName*/;

				// Grid для настроки номера сравниваемой модели
				var forwardModelNumGrid = new System.Windows.Controls.Grid();
				forwardModelNumGrid.ShowGridLines = false;
				forwardModelNumGrid.Margin = new Thickness(5, 5, 5, 0);
				forwardModelNumGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
				forwardModelNumGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
				forwardModelNumGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});
				forwardModelNumGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()
				{
					Width = System.Windows.GridLength.Auto
				});

				// TextBlock для ввода номера сравниваемой модели
				var textBlockForwardModelNum = new System.Windows.Controls.TextBlock();
				textBlockForwardModelNum.IsEnabled = true;
				textBlockForwardModelNum.Text = "Номер сравниваемой модели: ";

				// TextBox для ввода номера сравниваемой модели
				var textBoxForwardModelNum = new System.Windows.Controls.TextBox();
				textBoxForwardModelNum.IsEnabled = true;
				textBoxForwardModelNum.Width = 200;
				textBoxForwardModelNum.Margin = new Thickness(5, 5, 5, 5);
				textBoxForwardModelNum.Text = fileHandler.ForwardOdbModelVersionId/*forwardOdbModelVersionId*/;

				mainStackPanel.Children.Add(grBoxComparedModels);

				System.Windows.Controls.Grid.SetRow(textBlockServerName, 0);
				System.Windows.Controls.Grid.SetColumn(textBlockServerName, 0);

				System.Windows.Controls.Grid.SetRow(textBoxServerName, 0);
				System.Windows.Controls.Grid.SetColumn(textBoxServerName, 1);

				serverGrid.Children.Add(textBlockServerName);
				serverGrid.Children.Add(textBoxServerName);

				stackPanelComparedModels.Children.Add(serverGrid);

				System.Windows.Controls.Grid.SetRow(textBlockReverseModelContext, 0);
				System.Windows.Controls.Grid.SetColumn(textBlockReverseModelContext, 0);

				System.Windows.Controls.Grid.SetRow(textBoxReverseModelContext, 0);
				System.Windows.Controls.Grid.SetColumn(textBoxReverseModelContext, 1);

				reverseModelContextGrid.Children.Add(textBlockReverseModelContext);
				reverseModelContextGrid.Children.Add(textBoxReverseModelContext);

				stackPanelComparedModels.Children.Add(reverseModelContextGrid);


				System.Windows.Controls.Grid.SetRow(textBlockReverseModelNum, 0);
				System.Windows.Controls.Grid.SetColumn(textBlockReverseModelNum, 0);

				System.Windows.Controls.Grid.SetRow(textBoxReverseModelNum, 0);
				System.Windows.Controls.Grid.SetColumn(textBoxReverseModelNum, 1);

				reverseModelNumGrid.Children.Add(textBlockReverseModelNum);
				reverseModelNumGrid.Children.Add(textBoxReverseModelNum);

				stackPanelComparedModels.Children.Add(reverseModelNumGrid);


				System.Windows.Controls.Grid.SetRow(textBlockForwardModelContext, 0);
				System.Windows.Controls.Grid.SetColumn(textBlockForwardModelContext, 0);

				System.Windows.Controls.Grid.SetRow(textBoxForwardModelContext, 0);
				System.Windows.Controls.Grid.SetColumn(textBoxForwardModelContext, 1);

				forwardModelContextGrid.Children.Add(textBlockForwardModelContext);
				forwardModelContextGrid.Children.Add(textBoxForwardModelContext);

				stackPanelComparedModels.Children.Add(forwardModelContextGrid);


				System.Windows.Controls.Grid.SetRow(textBlockForwardModelNum, 0);
				System.Windows.Controls.Grid.SetColumn(textBlockForwardModelNum, 0);

				System.Windows.Controls.Grid.SetRow(textBoxForwardModelNum, 0);
				System.Windows.Controls.Grid.SetColumn(textBoxForwardModelNum, 1);

				forwardModelNumGrid.Children.Add(textBlockForwardModelNum);
				forwardModelNumGrid.Children.Add(textBoxForwardModelNum);

				stackPanelComparedModels.Children.Add(forwardModelNumGrid);

				grBoxComparedModels.Content = stackPanelComparedModels;

				// Button для выполнения действия
				var actionButton = new System.Windows.Controls.Button();
				actionButton.IsEnabled = true;
				actionButton.Height = 30;
				actionButton.Content = "Сравнить модели";

				mainStackPanel.Children.Add(actionButton);

				// UID текущей организации
				Guid OrgUid = Services.License.OrganizationUid;
				textBoxOrganisation.Text = OrgUid.ToString();


				// Запуск скрипта
				actionButton.Click += (sender, a) => {
					try
					{
						// Чтение данных
						fileHandler.OdbServerName/*_odbServerName*/ = textBoxServerName.Text;
						fileHandler.ReverseOdbInstanseName/*reverseOdbInstanseName*/ = textBoxReverseModelContext.Text;
						fileHandler.ReverseOdbModelVersionId/*reverseOdbModelVersionId*/ = textBoxReverseModelNum.Text;
						fileHandler.ForwardOdbInstanseName /*forwardOdbInstanseName*/ = textBoxForwardModelContext.Text;
						fileHandler.ForwardOdbModelVersionId /*forwardOdbModelVersionId*/ = textBoxForwardModelNum.Text;

						if (fileHandler.OdbServerName == String.Empty || fileHandler.ReverseOdbInstanseName == String.Empty || fileHandler.ReverseOdbModelVersionId == String.Empty ||
						fileHandler.ForwardOdbInstanseName == String.Empty || fileHandler.ForwardOdbModelVersionId == String.Empty)
						{
							throw new Exception("Введены не все данные для работы скрипта");
						}

						// Запись данных в config файл
						//string configFilePath = pathToScriptFiles + @"\EMSMCconfig.csv";
						System.IO.StreamWriter swConfig = new System.IO.StreamWriter(fileHandler.ConfigFilePath, false, System.Text.Encoding.Default);
						swConfig.WriteLine("OdbServerName;" + fileHandler.OdbServerName);
						swConfig.WriteLine("ReverseOdbInstanseName;" + fileHandler.ReverseOdbInstanseName);
						swConfig.WriteLine("ReverseOdbModelVersionId;" + fileHandler.ReverseOdbModelVersionId);
						swConfig.WriteLine("ForwardOdbInstanseName;" + fileHandler.ForwardOdbInstanseName);
						swConfig.WriteLine("ForwardOdbModelVersionId;" + fileHandler.ForwardOdbModelVersionId);
						swConfig.Close();

						// Подключение к исходной модели
						Monitel.Mal.Providers.MalContextParams reverseContext = new Monitel.Mal.Providers.MalContextParams()
						{
							OdbServerName = fileHandler.OdbServerName,
							OdbInstanseName = fileHandler.ReverseOdbInstanseName,
							OdbModelVersionId = Convert.ToInt32(fileHandler.ReverseOdbModelVersionId),
						};
						Monitel.Mal.Providers.Mal.MalProvider ReverseDataProvider = new Monitel.Mal.Providers.Mal.MalProvider(reverseContext, Monitel.Mal.Providers.MalContextMode.Open, "test", -1);
						reverseModelImage = new ModelImage(ReverseDataProvider, true);

						// Подключение к сравниваемой модели
						Monitel.Mal.Providers.MalContextParams forwardContext = new Monitel.Mal.Providers.MalContextParams()
						{
							OdbServerName = fileHandler.OdbServerName,
							OdbInstanseName = fileHandler.ForwardOdbInstanseName,
							OdbModelVersionId = Convert.ToInt32(fileHandler.ForwardOdbModelVersionId),
						};
						Monitel.Mal.Providers.Mal.MalProvider ForwardDataProvider = new Monitel.Mal.Providers.Mal.MalProvider(forwardContext, Monitel.Mal.Providers.MalContextMode.Open, "test", -1);
						forwardModelImage = new ModelImage(ForwardDataProvider, true);

						// Путь к реверс файлу
						string reversePath = programFolder.PathToScriptFiles + @"\ReverseHash.csv";
						System.IO.StreamWriter swReverse = new System.IO.StreamWriter(reversePath, false, System.Text.Encoding.Default);

						// Путь к форвард файлу
						string forwardPath = programFolder.PathToScriptFiles + @"\ForwardHash.csv";
						System.IO.StreamWriter swForward = new System.IO.StreamWriter(forwardPath, false, System.Text.Encoding.Default);

						// Путь к главному файлу с изменениями
						string differencePath = programFolder.PathToScriptFiles + @"\Перечень изменений.csv";
						System.IO.StreamWriter swDiff = new System.IO.StreamWriter(differencePath, false, System.Text.Encoding.Default);

						// Путь к файлу со свойствами, исключаемыми из проверки
						string exceptionPropertyListPath = programFolder.PathToScriptFiles + @"\ExceptionPropertyList.csv";
						System.IO.StreamReader srExcep = new System.IO.StreamReader(exceptionPropertyListPath, System.Text.Encoding.Default, false);

						HashSet<Guid> reverseObjectsUids = new HashSet<Guid>(); // Перечень объектов контроля из старой модели
						HashSet<Guid> forwardObjectsUids = new HashSet<Guid>(); // Перечень объектов контроля из новой модели



						exceptionPropertyHash = new HashSet<string>(); // Перечень свойст, которые исключаются из проверки

						// Поиск роли организации, по которой выполняется сравнение моделей
						Guid roleTypeUid;
						if (ComboBoxOrgRole.SelectedIndex == 0)
						{
							roleTypeUid = new Guid("1000161E-0000-0000-C000-0000006D746C"); // Контроль в МТН
							ODUSVCreatingListOfComparedObjectsMTN(reverseObjectsUids, ODUSVFindOrganisationRole(roleTypeUid, OrgUid), reverseModelImage);
							ODUSVCreatingListOfComparedObjectsMTN(forwardObjectsUids, ODUSVFindOrganisationRole(roleTypeUid, OrgUid), forwardModelImage);
						}
						else if (ComboBoxOrgRole.SelectedIndex == 1)
						{
							roleTypeUid = new Guid("10001672-0000-0000-C000-0000006D746C"); // Контроль в КПОС
							MessageBox.Show("Проверка изменений по объектам, контролируемым в КПОС, ещё не разработана");
						}
						else if (ComboBoxOrgRole.SelectedIndex == 2)
						{
							roleTypeUid = new Guid("10001669-0000-0000-C000-0000006D746C"); // Контроль в МУН
							MessageBox.Show("Проверка изменений по объектам, контролируемым в МУН, ещё не разработана");
						}

						// Запись списка объектов из проверяемых моделей в соответствующие файлы
						foreach (Guid uid in reverseObjectsUids)
						{
							swReverse.WriteLine(uid);
						}
						foreach (Guid uid in forwardObjectsUids)
						{
							swForward.WriteLine(uid);
						}

						// Составление списка со связями, исключенными из проверки
						string line;
						while ((line = srExcep.ReadLine()) != null)
						{
							exceptionPropertyHash.Add(line);
						}

						//закрыть текстовый файл для сохранения данных
						swReverse.Close();
						swForward.Close();
						srExcep.Close();


						MessageBox.Show("В списке исключений: " + exceptionPropertyHash.Count());
						MessageBox.Show("Добавлено reverse объектов: " + reverseObjectsUids.Count());
						MessageBox.Show("Добавлено forward объектов: " + forwardObjectsUids.Count());

						// Далее составляется список изменений в моделях
						listOfDifferences.Add("UID;Действие;Текст;Имя объекта;Класс объекта;Ответственный за моделирование");   // Шапка таблицы

						HashSet<Guid> deletedObjectsUids = new HashSet<Guid>(reverseObjectsUids);   // Перечень объектов удаленных из модели
						deletedObjectsUids.ExceptWith(forwardObjectsUids);

						MessageBox.Show("Удаленных объектов: " + deletedObjectsUids.Count());

						foreach (Guid uid in deletedObjectsUids)
						{
							ODUSVWriteAddedOrDeletedObject(uid, reverseModelImage, "deleted");
						}

						HashSet<Guid> addedObjectsUids = new HashSet<Guid>(forwardObjectsUids); // Перечень объектов добавленных в модель
						addedObjectsUids.ExceptWith(reverseObjectsUids);

						MessageBox.Show("Добавленных объектов: " + addedObjectsUids.Count());

						foreach (Guid uid in addedObjectsUids)
						{
							ODUSVWriteAddedOrDeletedObject(uid, forwardModelImage, "added");
						}

						// Добавление в список измененных объектов
						HashSet<Guid> otherMTNUids = new HashSet<Guid>(reverseObjectsUids); // Перечень объектов добавленных в модель
						otherMTNUids.IntersectWith(forwardObjectsUids);

						MessageBox.Show("Оставшихся объектов: " + otherMTNUids.Count());

						// Составление списка изменений
						foreach (Guid uid in otherMTNUids)
						{
							ODUSVFindChanges(uid);
						}


						// Заполнение файла с измененяим
						foreach (string text in listOfDifferences)
						{
							swDiff.WriteLine(text);
						}

						swDiff.Close();
						MessageBox.Show("Скрипт выполнен успешно");
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
						swReverse.Close();
						swForward.Close();
						swDiff.Close();
						srExcep.Close();
					}
				};



				//===================МЕТОДЫ===============================================

				Guid ODUSVFindOrganisationRole(Guid roleTypeUid, Guid organisationUid)
				{
					Guid result = new Guid();

					var organisation = forwardModelImage.GetObject<Organisation>(organisationUid);
					foreach (var role in organisation.Roles)
					{
						if (role.Category.Uid == roleTypeUid)
						{
							result = role.Uid;
							break;
						}
					}

					return result;
				}

				// Метод создания списка с UID'ами объектов, участвующих в МТН
				void ODUSVCreatingListOfComparedObjectsMTN(HashSet<Guid> hashOfObjects, /*OrganisationRole orgRole*/ Guid Uid, ModelImage mImg)
				{
					var mObjects = mImg.GetObjects<IdentifiedObject>();
					var orgRole = mImg.GetObject<OrganisationRole>(Uid);

					foreach (IdentifiedObject obj in orgRole.Objects)
					{
						// Добавляем контролируемое оборудование
						hashOfObjects.Add(obj.Uid);

						// Добавляем все дочерние объекты
						ODUSVCreatingListOfChildObjects(obj, hashOfObjects);
					}
				}

				// Рекурсивный метод добавления в список дочерних объектов
				void ODUSVCreatingListOfChildObjects(IdentifiedObject currentObj, HashSet<Guid> hashOfChildObjects)
				{
					if (currentObj.ChildObjects.Count() > 0)
					{
						foreach (IdentifiedObject childObj in currentObj.ChildObjects)
						{
							// Добавляем в список оборудование
							hashOfChildObjects.Add(childObj.Uid);

							// Проверяем и добавляем в список все связанные метеостанции и их дочерние объекты
							ODUSVFindWeatherStations(childObj, hashOfChildObjects);

							// Проверяем и добавляем в список все связанные уставки ступеней РЗА
							ODUSVFindPEStageSetPoints(childObj, hashOfChildObjects);

							// Рекурсивно добавляем в список все дочерние объекты
							ODUSVCreatingListOfChildObjects(childObj, hashOfChildObjects);
						}
					}
				}

				// Метод добавления в список связанных с оборудованием метеостанций
				void ODUSVFindWeatherStations(IdentifiedObject obj, HashSet<Guid> hashOfWeatherStations)
				{
					if (obj is Equipment)
					{
						foreach (WeatherStation ws in (obj as Equipment).WeatherStation)
						{
							hashOfWeatherStations.Add(ws.Uid);

							ODUSVCreatingListOfChildObjects(obj, hashOfWeatherStations);
						}
					}
					else if (obj is Terminal)
					{
						foreach (WeatherStation ws in (obj as Terminal).WeatherStations)
						{
							hashOfWeatherStations.Add(ws.Uid);

							ODUSVCreatingListOfChildObjects(obj, hashOfWeatherStations);
						}
					}
				}

				// Метод добавления в список уставок ступеней РЗА
				void ODUSVFindPEStageSetPoints(IdentifiedObject obj, HashSet<Guid> hashOfPEStageSetPoints)
				{
					if (obj is PEStage)
					{
						foreach (PEStageSetpoint pessp in (obj as PEStage).PESetpoints)
						{
							hashOfPEStageSetPoints.Add(pessp.Uid);
						}
					}
				}

				// Метод записи добавленного/удаленного объекта ИМ по UID в общий список изменений
				void ODUSVWriteAddedOrDeletedObject(Guid uid, ModelImage mImg, string state)
				{
					var selectedObject = mImg.GetObject<IdentifiedObject>(uid);
					switch (state)
					{
						case "deleted":
							listOfDifferences.Add(Convert.ToString(selectedObject.Uid) + ";Удаление;Удален объект класса " + selectedObject.ClassName() + ";" + selectedObject.name + ";" + selectedObject.ClassName());
							break;
						case "added":
							listOfDifferences.Add(Convert.ToString(selectedObject.Uid) + ";Добавление;Добавлен объект класса " + selectedObject.ClassName() + ";" + selectedObject.name + ";" + selectedObject.ClassName());
							break;
					}
				}


				void ODUSVFindChanges(Guid uid)
				{
					var reverseObject = reverseModelImage.GetObject</*IdentifiedObject*/IMalObject>(uid);
					// Проверка на принадлежность к именуемым объектам или уставкам РЗА
					if (reverseObject != null)
					{
						var forwardObject = forwardModelImage.GetObject</*IdentifiedObject*/IMalObject>(uid);

						// Если проверяемый объект - IMalObject
						if (reverseObject is IMalObject)
						{
							Type t = reverseObject.GetType();


							//(int)Convert.ChangeType(flt, typeof(int));
							var iMORev = reverseObject as IMalObject;
							var iMOForw = forwardObject as IMalObject;

							ODUSVPropertyReflectInfo(iMORev, iMOForw, exceptionPropertyHash, listOfDifferences);
						}

						#region             
						// Если проверяемый объект - участок линии переменного тока
						if (reverseObject is ACLineSegment)
						{
							ACLineSegment rObj = reverseObject as ACLineSegment;
							ACLineSegment fObj = forwardObject as ACLineSegment;
							ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
						}

						// Если проверяемый объект - аналог
						else if (reverseObject is Analog)
						{
							Analog rObj = reverseObject as Analog;
							Analog fObj = forwardObject as Analog;
							ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
						}

						// Если проверяемый объект - дискрет
						else if (reverseObject is Discrete)
						{
							Discrete rObj = reverseObject as Discrete;
							Discrete fObj = forwardObject as Discrete;
							ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
						}

						// Если проверяемый объект - ошиновка
						else if (reverseObject is BusArrangement)
						{
							BusArrangement rObj = reverseObject as BusArrangement;
							BusArrangement fObj = forwardObject as BusArrangement;
							ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
						}

						// Если проверяемый объект - набор эксплуатационных ограничений		
						else if (reverseObject is OperationalLimitSet)
						{
							OperationalLimitSet rObj = reverseObject as OperationalLimitSet;
							OperationalLimitSet fObj = forwardObject as OperationalLimitSet;
							ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
						}

						// Если проверяемый объект - предел тока		
						else if (reverseObject is CurrentLimit)
						{
							CurrentLimit cLRev = reverseObject as CurrentLimit;
							CurrentLimit cLForw = forwardObject as CurrentLimit;

							ODUSVPropertyReflectInfo(cLRev, cLForw, exceptionPropertyHash, listOfDifferences);
						}

						//CurrentFlowUnbalanceLimit
						// Если проверяемый объект - предел несимметрии токов фаз
						else if (reverseObject is CurrentFlowUnbalanceLimit)
						{
							CurrentFlowUnbalanceLimit cFULRev = reverseObject as CurrentFlowUnbalanceLimit;
							CurrentFlowUnbalanceLimit cFULForw = forwardObject as CurrentFlowUnbalanceLimit;

							ODUSVPropertyReflectInfo(cFULRev, cFULForw, exceptionPropertyHash, listOfDifferences);
						}

						// Если проверяемый объект - кривая зависимости тока от температуры
						else if (reverseObject is CurrentVsTemperatureLimitCurve)
						{
							CurrentVsTemperatureLimitCurve cVTLCRev = reverseObject as CurrentVsTemperatureLimitCurve;
							CurrentVsTemperatureLimitCurve cVTLCForw = forwardObject as CurrentVsTemperatureLimitCurve;

							ODUSVPropertyReflectInfo(cVTLCRev, cVTLCForw, exceptionPropertyHash, listOfDifferences);
						}

						//PotentialTransformer
						// Если проверяемый объект - трансформатор напряжения
						else if (reverseObject is PotentialTransformer)
						{
							PotentialTransformer pTRev = reverseObject as PotentialTransformer;
							PotentialTransformer pTForw = forwardObject as PotentialTransformer;

							ODUSVPropertyReflectInfo(pTRev, pTForw, exceptionPropertyHash, listOfDifferences);
						}

						// PotentialTransformerWinding
						// Если проверяемый объект - обмотка трансформатора напряжения
						else if (reverseObject is PotentialTransformerWinding)
						{
							PotentialTransformerWinding pTWRev = reverseObject as PotentialTransformerWinding;
							PotentialTransformerWinding pTWForw = forwardObject as PotentialTransformerWinding;

							ODUSVPropertyReflectInfo(pTWRev, pTWForw, exceptionPropertyHash, listOfDifferences);
						}

						// Terminal
						// Если проверяемый объект - полюс оборудования
						else if (reverseObject is Terminal)
						{
							Terminal tRev = reverseObject as Terminal;
							Terminal tForw = forwardObject as Terminal;

							ODUSVPropertyReflectInfo(tRev, tForw, exceptionPropertyHash, listOfDifferences);
						}

						// WaveTrap
						// Если проверяемый объект - ВЧЗ заградитель
						else if (reverseObject is WaveTrap)
						{
							WaveTrap wTRev = reverseObject as WaveTrap;
							WaveTrap wTForw = forwardObject as WaveTrap;

							ODUSVPropertyReflectInfo(wTRev, wTForw, exceptionPropertyHash, listOfDifferences);
						}

						// Breaker
						// Если проверяемый объект - выключатель
						else if (reverseObject is Breaker)
						{
							Breaker bRev = reverseObject as Breaker;
							Breaker bForw = forwardObject as Breaker;

							ODUSVPropertyReflectInfo(bRev, bForw, exceptionPropertyHash, listOfDifferences);
						}

						// AnalogValue
						// Если проверяемый объект - аналоговое значение
						else if (reverseObject is AnalogValue)
						{
							AnalogValue aVRev = reverseObject as AnalogValue;
							AnalogValue aVForw = forwardObject as AnalogValue;

							ODUSVPropertyReflectInfo(aVRev, aVForw, exceptionPropertyHash, listOfDifferences);
						}

						// DiscreteValue
						// Если проверяемый объект - дискретное значение
						else if (reverseObject is DiscreteValue)
						{
							DiscreteValue dVRev = reverseObject as DiscreteValue;
							DiscreteValue dVForw = forwardObject as DiscreteValue;

							ODUSVPropertyReflectInfo(dVRev, dVForw, exceptionPropertyHash, listOfDifferences);
						}

						// CurrentTransformer
						// Если проверяемый объект - трансформатор тока
						else if (reverseObject is CurrentTransformer)
						{
							CurrentTransformer cTRev = reverseObject as CurrentTransformer;
							CurrentTransformer cTForw = forwardObject as CurrentTransformer;

							ODUSVPropertyReflectInfo(cTRev, cTForw, exceptionPropertyHash, listOfDifferences);
						}

						// CurrentTransformerWinding
						// Если проверяемый объект - обмотка трансформатора тока
						else if (reverseObject is CurrentTransformerWinding)
						{
							CurrentTransformerWinding cTWRev = reverseObject as CurrentTransformerWinding;
							CurrentTransformerWinding cTWForw = forwardObject as CurrentTransformerWinding;

							ODUSVPropertyReflectInfo(cTWRev, cTWForw, exceptionPropertyHash, listOfDifferences);
						}

						// Disconnector
						// Если проверяемый объект - разъединитель
						else if (reverseObject is Disconnector)
						{
							Disconnector dRev = reverseObject as Disconnector;
							Disconnector dForw = forwardObject as Disconnector;

							ODUSVPropertyReflectInfo(dRev, dForw, exceptionPropertyHash, listOfDifferences);
						}

						// Line
						// Если проверяемый объект - ЛЭП
						else if (reverseObject is Line)
						{
							Line lRev = reverseObject as Line;
							Line lForw = forwardObject as Line;

							ODUSVPropertyReflectInfo(lRev, lForw, exceptionPropertyHash, listOfDifferences);
						}

						// PowerTransformer
						// Если проверяемый объект - силовой трансформатор
						else if (reverseObject is PowerTransformer)
						{
							PowerTransformer pTRev = reverseObject as PowerTransformer;
							PowerTransformer pTForw = forwardObject as PowerTransformer;

							ODUSVPropertyReflectInfo(pTRev, pTForw, exceptionPropertyHash, listOfDifferences);
						}

						// PowerTransformerEnd
						// Если проверяемый объект - обмотка трансформатора
						else if (reverseObject is PowerTransformerEnd)
						{
							PowerTransformerEnd pTERev = reverseObject as PowerTransformerEnd;
							PowerTransformerEnd pTEForw = forwardObject as PowerTransformerEnd;

							ODUSVPropertyReflectInfo(pTERev, pTEForw, exceptionPropertyHash, listOfDifferences);
						}

						// CurrentVsTapStepLimitCurve
						// Если проверяемый объект - кривая зависимости тока от регулировачного ответвления
						else if (reverseObject is CurrentVsTapStepLimitCurve)
						{
							CurrentVsTapStepLimitCurve cVTSLCRev = reverseObject as CurrentVsTapStepLimitCurve;
							CurrentVsTapStepLimitCurve cVTSLCForw = forwardObject as CurrentVsTapStepLimitCurve;

							ODUSVPropertyReflectInfo(cVTSLCRev, cVTSLCForw, exceptionPropertyHash, listOfDifferences);
						}

						// RatioTapChanger
						// Если проверяемый объект - ПБВ/РПН
						else if (reverseObject is RatioTapChanger)
						{
							RatioTapChanger rTCRev = reverseObject as RatioTapChanger;
							RatioTapChanger rTCForw = forwardObject as RatioTapChanger;

							ODUSVPropertyReflectInfo(rTCRev, rTCForw, exceptionPropertyHash, listOfDifferences);
						}

						// TransformerMeshImpedance
						// Если проверяемый объект - трансформаторная ветвь многоугольника
						else if (reverseObject is TransformerMeshImpedance)
						{
							TransformerMeshImpedance tMIRev = reverseObject as TransformerMeshImpedance;
							TransformerMeshImpedance tMIForw = forwardObject as TransformerMeshImpedance;

							ODUSVPropertyReflectInfo(tMIRev, tMIForw, exceptionPropertyHash, listOfDifferences);
						}

						// LoadSheddingEquipment
						// Если проверяемый объект - автоматика ограничения потребления
						else if (reverseObject is LoadSheddingEquipment)
						{
							LoadSheddingEquipment lSERev = reverseObject as LoadSheddingEquipment;
							LoadSheddingEquipment lSEForw = forwardObject as LoadSheddingEquipment;

							ODUSVPropertyReflectInfo(lSERev, lSEForw, exceptionPropertyHash, listOfDifferences);
						}

						// GenericPSR
						// Если проверяемый объект - прочий энергообъект
						else if (reverseObject is GenericPSR)
						{
							GenericPSR gPSRRev = reverseObject as GenericPSR;
							GenericPSR gPSRForw = forwardObject as GenericPSR;

							ODUSVPropertyReflectInfo(gPSRRev, gPSRForw, exceptionPropertyHash, listOfDifferences);
						}

						// GenerationUnloadingStage
						// Если проверяемый объект - ступень ОГ
						else if (reverseObject is GenerationUnloadingStage)
						{
							GenerationUnloadingStage gUSRev = reverseObject as GenerationUnloadingStage;
							GenerationUnloadingStage gUSForw = forwardObject as GenerationUnloadingStage;

							ODUSVPropertyReflectInfo(gUSRev, gUSForw, exceptionPropertyHash, listOfDifferences);
						}

						// LoadSheddingStage
						// Если проверяемый объект - ступень ОН
						else if (reverseObject is LoadSheddingStage)
						{
							LoadSheddingStage lSSRev = reverseObject as LoadSheddingStage;
							LoadSheddingStage lSSForw = forwardObject as LoadSheddingStage;

							ODUSVPropertyReflectInfo(lSSRev, lSSForw, exceptionPropertyHash, listOfDifferences);
						}

						// LimitExpression
						// Если проверяемый объект - выражение эксплуатационного ограничения
						else if (reverseObject is LimitExpression)
						{
							LimitExpression lERev = reverseObject as LimitExpression;
							LimitExpression lEForw = forwardObject as LimitExpression;

							ODUSVPropertyReflectInfo(lERev, lEForw, exceptionPropertyHash, listOfDifferences);
						}

						// PSRMeasOperand
						// Если проверяемый объект - операнд измерения энергообъекта
						else if (reverseObject is PSRMeasOperand)
						{
							PSRMeasOperand pSRMORev = reverseObject as PSRMeasOperand;
							PSRMeasOperand pSRMOForw = forwardObject as PSRMeasOperand;

							ODUSVPropertyReflectInfo(pSRMORev, pSRMOForw, exceptionPropertyHash, listOfDifferences);
						}

						// PEStageSetpoint
						// Если проверяемый объект - уставка ступени РЗА
						else if (reverseObject is PEStageSetpoint)
						{
							PEStageSetpoint pESSRev = reverseObject as PEStageSetpoint;
							PEStageSetpoint pESSForw = forwardObject as PEStageSetpoint;

							ODUSVPropertyReflectInfo(pESSRev, pESSForw, exceptionPropertyHash, listOfDifferences);
						}

						// WeatherStation
						// Если проверяемый объект - метеостанция
						else if (reverseObject is WeatherStation)
						{
							WeatherStation wSRev = reverseObject as WeatherStation;
							WeatherStation wSForw = forwardObject as WeatherStation;

							ODUSVPropertyReflectInfo(wSRev, wSForw, exceptionPropertyHash, listOfDifferences);
						}

						#endregion
					}
					else
					{
						try
						{
							// PEStageSetpoint
							// Если проверяемый объект - уставка ступени РЗА
							var reversPESetpoint = reverseModelImage.GetObject<PEStageSetpoint>(uid);

							if (reversPESetpoint != null)
							{
								var forwardPESetpoint = forwardModelImage.GetObject<PEStageSetpoint>(uid);
								ODUSVPropertyReflectInfo(reversPESetpoint, forwardPESetpoint, exceptionPropertyHash, listOfDifferences);
							}
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message + "Vot zdes ");
						}
					}

				}

				// Рефлекся. Метод перебирает все свойства класса
				void ODUSVPropertyReflectInfo<T>(T objRev, T objForw, HashSet<string> exHash, List<string> listOfDiffs) where T : class
				{
					// В переменную задается тип выбранного объекта
					Type t = typeof(T);

					// В переменную заносятся все реализуемые выбранным классом интерфейсы
					var interfaces = t.GetInterfaces();
					// Циклический проход по всем интерфейсам
					foreach (Type interf in interfaces)
					{
						// Дальнейшую обработку проходят все интерфейсы, кроме IMalObject
						if (interf.Name != "IMalObject")
						{
							// Циклический проход по всем свойствам выбранного интерфейса
							foreach (System.Reflection.PropertyInfo prop in interf.GetProperties())
							{
								ODUSVCheckingIdentityOfProperty(exHash, listOfDiffs, objRev, objForw, prop);
							}
						}

					}

					// В переменную заносятся все имеющиеся у выбранного класса свойства (только
					// те, что имеются у этого класса, а не наследуются)
					System.Reflection.PropertyInfo[] propNames = t.GetProperties();

					// Циклический проход по всем свойствам выбранного класса
					foreach (System.Reflection.PropertyInfo prop in propNames)
					{
						ODUSVCheckingIdentityOfProperty(exHash, listOfDiffs, objRev, objForw, prop);
					}
				}

				// Метод проверки идентичности свойства у двух объектов ИМ
				void ODUSVCheckingIdentityOfProperty<T>(HashSet<string> _exHash, List<string> _listOfDiffs, T _objRev, T _objForw, System.Reflection.PropertyInfo _prop) where T : class
				{
					try
					{
						// В переменную задается тип выбранного объекта
						Type t = typeof(T);

						// Проверка отсуствия выбранного свойства в списке исключений
						if (_exHash.Contains(_prop.Name) != true)
						{
							// В переменную задается значение проверяемого свойства выбранного объекта
							var vals = _prop.GetValue(_objRev);
							var valsForw = _prop.GetValue(_objForw);

							// Если выбранное свойство содержит массив данных, то выполняется обработка массива
							if (vals is Array)
							{
								// Если выбранное свойство - это объект типа System.Byte[], 
								// то выполняется проверка объекта на принадлежность к классу Curve
								if (_prop.PropertyType.ToString() == "System.Byte[]" && _objRev is Curve)
								{
									// Получаем точки каждой кривой в обеих моделях
									var curvePointsRev = (_objRev as Curve).GetCurvePoints();
									var curvePointsForw = (_objForw as Curve).GetCurvePoints();

									string stringOfPointsRev = String.Empty;
									string stringOfPointsForw = String.Empty;

									// По аналогии с проверкой связи один ко многим создаем из координат точек сплошные строки и сравниваем их
									for (int i = 0; i < curvePointsRev.xvalue.Count(); i++)
									{ stringOfPointsRev = stringOfPointsRev + curvePointsRev.xvalue[i] + curvePointsRev.y1value[i]; }
									for (int i = 0; i < curvePointsForw.xvalue.Count(); i++)
									{ stringOfPointsForw = stringOfPointsForw + curvePointsForw.xvalue[i] + curvePointsForw.y1value[i]; }

									if (stringOfPointsRev != stringOfPointsForw)
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
												"' изменилось значение в свойстве '" + _prop.Name + "'. Было " + "points1" + ", стало " +
												"points2" + ";" + ODUSVGetObjectName(_objRev) + ";" +
												(_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));
								}

								else if (_prop.PropertyType.ToString() == "Monitel.Mal.Context.CIM16.BranchGroupTerminal[]" && _objRev is Terminal)
								{
									// ПЕРЕПИСАТЬ В ОТДЕЛЬНЫЙ МЕТОД
									// Добавление UID'ов объектов в сортированный список
									SortedList<string, string> reverseList = new SortedList<string, string>();
									SortedList<string, string> forwardList = new SortedList<string, string>();

									foreach (IMalObject val in vals as Array)
									{
										reverseList.Add(val.Uid.ToString(), val.Uid.ToString());
									}
									foreach (IMalObject val in valsForw as Array)
									{
										forwardList.Add(val.Uid.ToString(), val.Uid.ToString());
									}

									// Составление строк идентификаторов у каждого из проверяемых объектов
									string reverseCode = "";
									string forwardCode = "";
									foreach (string str in reverseList.Keys)
									{
										reverseCode = reverseCode + str;
									}
									foreach (string str in forwardList.Keys)
									{
										forwardCode = forwardCode + str;
									}

									if (reverseCode != forwardCode)
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
											"' изменилась связь " + _prop.Name + ";" + ODUSVGetObjectName(_objRev) + ";" +
											(_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));
								}

								else
								{
									// Проверка значений в обеих моделях на null
									if (_prop.GetValue(_objRev) == null && _prop.GetValue(_objForw) != null)
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
										"' изменилась связь " + _prop.Name + ";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() +
										";" + ODUSVGetModelingAuthoritySet(_objRev));

									if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) == null)
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
										"' изменилась связь " + _prop.Name + ";" + ODUSVGetObjectName(_objRev) + ";" +
										(_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));

									if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) != null)
									{
										// Добавление UID'ов объектов в сортированный список
										SortedList<string, string> reverseList = new SortedList<string, string>();
										SortedList<string, string> forwardList = new SortedList<string, string>();

										foreach (IMalObject val in vals as Array)
										{
											reverseList.Add(val.Uid.ToString(), val.Uid.ToString());
										}
										foreach (IMalObject val in valsForw as Array)
										{
											forwardList.Add(val.Uid.ToString(), val.Uid.ToString());
										}

										// Составление строк идентификаторов у каждого из проверяемых объектов
										string reverseCode = "";
										string forwardCode = "";
										foreach (string str in reverseList.Keys)
										{
											reverseCode = reverseCode + str;
										}
										foreach (string str in forwardList.Keys)
										{
											forwardCode = forwardCode + str;
										}

										if (reverseCode != forwardCode)
											_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
												"' изменилась связь " + _prop.Name + ";" + ODUSVGetObjectName(_objRev) + ";" +
												(_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));
									}
								}
							}
							// Если выбранное свойство - это не массив данных
							else
							{
								// Если выбранное свойство строковое, числовое или булево
								if (_prop.PropertyType.ToString() == "System.String" || _prop.PropertyType.ToString() == "System.Boolean" ||
											_prop.PropertyType.ToString() == "System.Nullable`1[System.Double]" || _prop.PropertyType.ToString() == "System.Double"
											|| _prop.PropertyType.ToString() == "System.Int32" || _prop.PropertyType.ToString() == "System.Int64")
								{

									// Проверка значений в обеих моделях на null
									if (_prop.GetValue(_objRev) == null && _prop.GetValue(_objForw) != null)
									{
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
										"' изменилось значение в свойстве '" + _prop.Name + "'. Было 'не задан'" + ", стало " + _prop.GetValue(_objForw).ToString().Replace("\r\n", "").Replace("\n", "") +
										";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() + ";" +
										ODUSVGetModelingAuthoritySet(_objRev));
									}
									else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) == null)
									{
										if ((_objRev as IdentifiedObject).Uid == new Guid("A8DFF2C0-68F3-4F97-B0AE-58ABAE838B00")) MessageBox.Show(_prop.Name + " = " + _prop.GetValue(_objRev));
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
										"' изменилось значение в свойстве '" + _prop.Name + "'. Было " + _prop.GetValue(_objRev).ToString().Replace("\r\n", "").Replace("\n", "") + ", стало 'не задан'" +
										";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() + ";" +
										ODUSVGetModelingAuthoritySet(_objRev));
									}
									else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) != null)
									{
										if (_prop.GetValue(_objRev).ToString() != _prop.GetValue(_objForw).ToString())
										{
											if (_objRev is IdentifiedObject && _objForw is IdentifiedObject)
												_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
												"' изменилось значение в свойстве '" + _prop.Name + "'. Было " + _prop.GetValue(_objRev).ToString().Replace("\r\n", "").Replace("\n", "") + ", стало " +
												_prop.GetValue(_objForw).ToString().Replace("\r\n", "").Replace("\n", "") + ";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() +
												";" + ODUSVGetModelingAuthoritySet(_objRev));
											else
												_listOfDiffs.Add(Convert.ToString((_objRev as IMalObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
												"' изменилось значение в свойстве '" + _prop.Name + "'. Было " + _prop.GetValue(_objRev).ToString().Replace("\r\n", "").Replace("\n", "") + ", стало " +
												_prop.GetValue(_objForw).ToString().Replace("\r\n", "").Replace("\n", "") + ";" + (_objRev as IMalObject).ClassName()/*name*/ + ";" + (_objRev as IMalObject).ClassName() +
												";" + ODUSVGetModelingAuthoritySet(_objRev));
										}
									}
								}

								// Если выбранное свойство - это объект какого-то класса
								else
								{
									// Проверка значений в обеих моделях на null
									if (_prop.GetValue(_objRev) == null && _prop.GetValue(_objForw) != null)
									{
										string line = "";
										// Проверка на полюс
										if (valsForw is Terminal)
										{
											line = ODUSVGetObjectName(valsForw);

										}
										else if (valsForw is IdentifiedObject)
										{
											line = ODUSVGetObjectName(valsForw);
										}
										else line = ODUSVGetObjectName(valsForw);

										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
										"' изменилось значение в свойстве '" + _prop.Name + "'. Было 'не задан'" + ", стало " + line +
										";" + ODUSVGetObjectName(_objRev) + ";" + (_objRev as IdentifiedObject).ClassName() + ";" +
										ODUSVGetModelingAuthoritySet(_objRev));
									}
									else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) == null)
									{
										string line = "";
										// Проверка на полюс
										if (vals is Terminal)
											line = ODUSVGetObjectName(vals);
										else if (vals is IdentifiedObject)
											line = ODUSVGetObjectName(vals);
										else line = ODUSVGetObjectName(vals);
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
											"' изменилось значение в свойстве '" + _prop.Name + "'. Было " + line + ", стало 'не задан'" +
											";" + ODUSVGetObjectName(_objRev) + ";" + (_objRev as IdentifiedObject).ClassName() + ";"
											+ ODUSVGetModelingAuthoritySet(_objRev));
									}
									else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) != null &&
												_prop.GetValue(_objRev).ToString() != _prop.GetValue(_objForw).ToString())
									{
										string line1 = "";
										string line2 = "";
										// Проверка на полюс
										if (vals is Terminal)
											line1 = ODUSVGetObjectName(vals);
										else if (vals is IdentifiedObject)
											line1 = ODUSVGetObjectName(vals);
										else line1 = ODUSVGetObjectName(vals);

										if (valsForw is Terminal)
											line2 = ODUSVGetObjectName(valsForw);
										else if (valsForw is IdentifiedObject)
											line2 = ODUSVGetObjectName(valsForw);
										else line2 = ODUSVGetObjectName(valsForw);

										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";Изменение;У объекта класса '" + t.ToString() +
												"' изменилось значение в свойстве '" + _prop.Name + "'. Было " + line1 + ", стало " +
												line2 + ";" + ODUSVGetObjectName(_objRev) + ";" + (_objRev as IdentifiedObject).ClassName() +
												";" + ODUSVGetModelingAuthoritySet(_objRev));
									}
								}
							}

						}
					}
					catch (Exception e)
					{
						MessageBox.Show(e.ToString() + "\n" + _prop.Name + "\n" + _prop.PropertyType.ToString() + "\n" + (_objRev as IMalObject).Uid);
					}
				}



				// Метод определения имении объекта ИМ
				string ODUSVGetObjectName<T>(/*IMalObject*/T inputObject) where T : class
				{
					string result = String.Empty;
					if (inputObject is IMalObject)
					{
						// Переопрделяем объект для дальнейшей работы
						IMalObject obj = inputObject as IMalObject;

						// Если объект является именуемым объектом, но не полюсом и не точкой подключения
						if ((obj is IdentifiedObject) == true && (obj is Terminal) == false && (obj is ConnectivityNode) == false)
						{
							result = (obj as IdentifiedObject).name;
						}
						// Если объект является полюсом
						else if (obj is Terminal)
						{
							result = "T" + (obj as Terminal).sequenceNumber + " " + (obj as Terminal).ConductingEquipment.name;
						}
						// Если объект является именуемым объектом, но не полюсом и не точкой подключения
						else if (obj is ConnectivityNode)
						{
							result = "ConnectivityNode : " + obj.Id + " " + obj.Uid;
						}
						// Если объект не является именуемым
						else if ((obj is IdentifiedObject) == false)
						{
							result = Convert.ToString(obj.Uid);
						}
					}
					return result;
				}


				string ODUSVGetModelingAuthoritySet<T>(T inputObject)
				{
					string result = String.Empty;
					IdentifiedObject idObj = null;

					if (inputObject is PEStageSetpoint)
						idObj = (Terminal)(inputObject as PEStageSetpoint).Terminal;
					else if (inputObject is IdentifiedObject)
						idObj = inputObject as IdentifiedObject;

					if (idObj != null)
					{
						if (idObj.ModelingAuthoritySet != null)
							result = idObj.ModelingAuthoritySet.name;
						else
							result = ODUSVGetModelingAuthoritySet(idObj.ParentObject);
					}

					return result;
				}


				//===========МОЁ (КОНЕЦ)===============================================================



				dynamic dockLayoutManager = (MainWindow.Content as System.Windows.Controls.Grid).Children[2];
				var dockingAssemName = "DevExpress.Xpf.Docking.v21.2, Version=21.2.5.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a";
				System.Reflection.Assembly dockingAssem = System.Reflection.Assembly.Load(dockingAssemName);


				Type layoutPanelType = dockingAssem.GetType("DevExpress.Xpf.Docking.LayoutPanel");
				Type layoutGroupType = dockingAssem.GetType("DevExpress.Xpf.Docking.LayoutGroup");
				Type tabbedGroupType = dockingAssem.GetType("DevExpress.Xpf.Docking.TabbedGroup");

				Type type2 = dockingAssem.GetType("DevExpress.Xpf.Docking.BaseLayoutItem");
				var group = dockLayoutManager.LayoutRoot.Items;

				var lst = new System.Collections.ArrayList();
				var groupIn = true;
				var groupFound = true;

				lst.Add(dockLayoutManager.LayoutRoot);
				var it = 0;
				var properties = dockLayoutManager.LayoutRoot;

				while (groupFound && it < 50)
				{
					it++;
					groupFound = false;
					var newlst = new System.Collections.ArrayList();
					foreach (dynamic t in lst)
					{

						foreach (var t2 in t.Items)
						{
							if (t2.ToString() == layoutGroupType.ToString() || t2.ToString() == tabbedGroupType.ToString())
							{
								if (!lst.Contains(t2))
								{
									newlst.Add(t2);
									groupFound = true;
									break;
								}
							}
						}
					}
					lst.AddRange(newlst);

				}
				foreach (dynamic gr in lst)
				{
					foreach (var g in gr.Items)
					{

						if (g.Name == "propertyGrid")
						{
							properties = g;

						}
					}
				}

				dynamic scriptWindow = Activator.CreateInstance(layoutPanelType);
				scriptWindow.Caption = "Сравнение моделей EMS";
				scriptWindow.Content = scroll;
				scriptWindow.ItemWidth = new System.Windows.GridLength(407);


				var group2 = properties.Parent;


				if (properties.Parent.ToString() == layoutGroupType.ToString())
				{
					dynamic tabGroup = Activator.CreateInstance(tabbedGroupType);
					tabGroup.Add(properties);
					tabGroup.Add(scriptWindow);
					tabGroup.SelectedTabIndex = properties.Parent.Items.Count - 1;
					tabGroup.ItemWidth = new System.Windows.GridLength(407);
					dockLayoutManager.DockController.Insert(group2, tabGroup, 0);
				}
				else
				{

					properties.Parent.Add(scriptWindow);
					properties.Parent.SelectedTabIndex = properties.Parent.Items.Count - 1;
				}

			}
			catch (Exception ex) { MessageBox.Show(ex.Message); }
		}
    }
}
