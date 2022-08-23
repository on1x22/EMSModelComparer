using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text;
using System.Threading.Tasks;
using Monitel.Mal;
using Monitel.DataContext.Tools.ModelExtensions;
using Monitel.Mal.Context.CIM16;
using Monitel.Mal.Context.CIM16.Ext.EMS;

namespace EMSModelComparer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IView
    {
		//Window MainWindow;
		Window mainWindow;
		System.Windows.Controls.ScrollViewer scroll;
		//System.Windows.Controls.StackPanel mainStackPanel;
		/*FileHandler fileHandler;
		FolderWithAppFilesHandler programFolder;*/

		//System.Windows.Controls.Button actionButton;
		//System.Windows.Controls.TextBox textBoxServerName;
		//System.Windows.Controls.TextBox textBoxReverseModelContext;
		//System.Windows.Controls.TextBox textBoxReverseModelNum;
		//System.Windows.Controls.TextBox textBoxForwardModelContext;
		//System.Windows.Controls.TextBox textBoxForwardModelNum;
		//System.Windows.Controls.ComboBox ComboBoxOrgRole;
		//System.Windows.Controls.TextBox textBoxOrganisation;

		public string ServerName
		{
			get { return textBoxServerName.Text; }
		}

		public string ReverseModelContext
		{
			get { return textBoxReverseModelContext.Text; }
		}
		public string ReverseModelId
		{
			get { return textBoxReverseModelId.Text; }
		}
		public string ForwardModelContext
		{
			get { return textBoxForwardModelContext.Text; }
		}
		public string ForwardModelId
		{
			get { return textBoxForwardModelId.Text; }
		}

		public string OrganisationUid
		{
			get { return textBoxOrganisation.Text; }
			private set { }
		}
		public int OrganisationRoleIndex
		{
			get { return comboBoxOrganisationRole.SelectedIndex; }
		}

		public bool IsFileOpen
        {
			get { return (bool)checkBoxIsFileOpen.IsChecked; }
		}

		/*ModelImage reverseModelImage;
		ModelImage forwardModelImage;

		// Список с изменениями в модели
		List<string> listOfDifferences;
		HashSet<string> exceptionPropertyHash;

		// Потоки для работы с файлами		
		System.IO.StreamWriter swReverse;
		System.IO.StreamWriter swForward;
		System.IO.StreamWriter swDiff;
		System.IO.StreamReader srExcep;*/

		//IServiceManager Services;

		//Guid OrganisationUid;

		IModel model;
		IController controller;

		internal MainWindow(/*Window MainWindow, IServiceManager Services,*/ IModel model, IController controller)
		{            
            InitializeComponent();

			//this.MainWindow = MainWindow;
			//this.Services = Services;
			this.model = model;
			this.controller = controller;
			GetConfiguration();
			//Show();
		}

		//TODO: Метод не используется. Необходимо удалить 
		public void InitializeWindow()
		{
            #region
            /*//Создание главного окна, scroll и StackPanel
			mainWindow = new Window();
			scroll = new System.Windows.Controls.ScrollViewer();
			mainStackPanel = new System.Windows.Controls.StackPanel();

			//Инициализация главного окна
			mainWindow.Title = "Окно замечаний";
			mainWindow.Width = 750;
			mainWindow.MaxWidth = 750;
			mainWindow.Content = scroll;
			mainWindow.ResizeMode = ResizeMode.NoResize;
			mainWindow.SizeToContent = SizeToContent.Height;

			// Инициализация элемента scroll
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

			var expander = new System.Windows.Controls.Expander(); // Вопросище????

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
			textBoxOrganisation = new System.Windows.Controls.TextBox();
			textBoxOrganisation.IsEnabled = true;
			textBoxOrganisation.Margin = new Thickness(5, 5, 5, 5);
			textBoxOrganisation.Text = null;

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

			ComboBoxOrgRole = new System.Windows.Controls.ComboBox();
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
			textBoxServerName = new System.Windows.Controls.TextBox();
			textBoxServerName.IsEnabled = true;
			textBoxServerName.Width = 200;
			textBoxServerName.Margin = new Thickness(5, 5, 5, 5);
			textBoxServerName.Text = null;

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
			textBoxReverseModelContext = new System.Windows.Controls.TextBox();
			textBoxReverseModelContext.IsEnabled = true;
			textBoxReverseModelContext.Width = 200;
			textBoxReverseModelContext.Margin = new Thickness(5, 5, 5, 5);
			textBoxReverseModelContext.Text = null;

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
			textBoxReverseModelNum = new System.Windows.Controls.TextBox();
			textBoxReverseModelNum.IsEnabled = true;
			textBoxReverseModelNum.Width = 200;
			textBoxReverseModelNum.Margin = new Thickness(5, 5, 5, 5);
			textBoxReverseModelNum.Text = null;

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
			textBoxForwardModelContext = new System.Windows.Controls.TextBox();
			textBoxForwardModelContext.IsEnabled = true;
			textBoxForwardModelContext.Width = 200;
			textBoxForwardModelContext.Margin = new Thickness(5, 5, 5, 5);
			textBoxForwardModelContext.Text = null;

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
			textBoxForwardModelNum = new System.Windows.Controls.TextBox();
			textBoxForwardModelNum.IsEnabled = true;
			textBoxForwardModelNum.Width = 200;
			textBoxForwardModelNum.Margin = new Thickness(5, 5, 5, 5);
			textBoxForwardModelNum.Text = null;

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

			grBoxComparedModels.Content = stackPanelComparedModels;*/

            /*// Button для выполнения действия
			actionButton = new System.Windows.Controls.Button();
			actionButton.IsEnabled = true;
			actionButton.Height = 30;
			actionButton.Content = "Сравнить модели";

			mainStackPanel.Children.Add(actionButton);*/

            /*// UID текущей организации
			OrgUid = Services.License.OrganizationUid;
			textBoxOrganisation.Text = OrgUid.ToString();*/
            #endregion

            //InitializeDevExpress();

            //GetConfiguration();
		}

		

		internal void GetConfiguration()
		{
			textBoxServerName.Text = model.OdbServerName;
			textBoxReverseModelContext.Text = model.ReverseOdbInstanseName;
			textBoxReverseModelId.Text = model.ReverseOdbModelVersionId;
			textBoxForwardModelContext.Text = model.ForwardOdbInstanseName;
			textBoxForwardModelId.Text = model.ForwardOdbModelVersionId;
			// UID текущей организации
			//OrganisationUid = Services.License.OrganizationUid;
			textBoxOrganisation.Text = model.OrganisationUid.ToString();
		}


		private void Start()
		{
			
		}

		private void CheckInputData()
		{
			if (textBoxServerName.Text == String.Empty ||
				textBoxReverseModelContext.Text == String.Empty ||
				textBoxReverseModelId.Text == String.Empty ||
				textBoxForwardModelContext.Text == String.Empty ||
				textBoxForwardModelId.Text == String.Empty ||
				textBoxOrganisation.Text == String.Empty)
			{
				throw new Exception("Введены не все данные для работы скрипта");
			}
		}

        private void actionButton_Click(object sender, RoutedEventArgs e)
        {
			CheckInputData();
			// Запуск скрипта
			controller.StartComparison();
        }

        /*private void InitializeDevExpress()
		{
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

		}*/
    }
}
