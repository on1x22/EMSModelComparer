using System;
using System.Collections.Generic;
using System.Windows;
using Monitel.Mal;
using Monitel.DataContext.Tools.ModelExtensions;
using Monitel.Mal.Context.CIM16;
using Monitel.Mal.Context.CIM16.Ext.EMS;
using Monitel.UI.Infrastructure.Services;


namespace EMSModelComparer
{
    internal class Program
    {
        static void Main(string[] args)
        {
			/// v0.1. (21.02.2022) ���������
			///
			/// ��� �������������� �������� ��������� � ����� "���������" ��������� ����� "EMS model comparer", � ����� �����, ����������� ��� ������ ���������.
			///	������������ ������� �������� � �������: ag-lis-aipim.odusv.so (�������� ����� ������ � ��������� ������ ������ �� ���� ���\���������� � ������\������).
			/// ������������ �� �������� � �������: ODB_EnergyMain (�������� ����� ������ � ��������� ������ ������ �� ���� ���\���������� � ������\��� ���� ������).
			/// ��� ������� ���������� ������� ���������� � ������������ ������� ������������ � ���� EMSMCconfig.csv. ��� ����������� �������� ��������� ����������� ������
			/// ������������� ��������� �����.
			///
			/// �������� ������ ���������:
			/// 1. � �������� ������� ����������� ����� �������� �� ��������� ���� (�� + ��� ����), � ����� ���� �������� � ��������� �������� ��, ����������� ��� ������ 
			///    ��������������� ��������� ��-11 (���, ����, ���). ��������� ������� �������� � ������ ��������� � ������������ � ����� ReverseHash.csv � ForwardHash.csv (��� �������);
			/// 2. ����������� ������ �� ������� ��������, ������� ������� �������� ������ � ������������ ��������� � ������������ ���������� ������� � ������������ ������. ���������� 
			///    ��������� ������� ������������ � ���� �������� ���������.csv;
			/// 3. ��� ������� �������� ������� � ����� ExceptionPropertyList.csv ��� �������� ������������ � ���� ������������ �������� � � �������� ���� � ����������� ����������� �� 
			///    ��������.
			/// 4. ���������/������/����������� �� ������ ��������� ����� ���������� �� ����� alehinra@odusv.so-ups.ru.

			try
			{
				// ���� � ������ �������
				var pathToScriptFiles = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EMS model comparer";

				string md = Environment.GetFolderPath(Environment.SpecialFolder.Personal);//���� � ����������
				if (System.IO.Directory.Exists(pathToScriptFiles) == false)
				{
					System.IO.Directory.CreateDirectory(pathToScriptFiles);
				}

				//string mydocu = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

				// �������� ������� � ������ �������� �������
				var _odbServerName = String.Empty;
				var reverseOdbInstanseName = String.Empty;
				var reverseOdbModelVersionId = String.Empty;
				var forwardOdbInstanseName = String.Empty;
				var forwardOdbModelVersionId = String.Empty;

				string configFilePath = pathToScriptFiles + @"\EMSMCconfig.csv";
				System.IO.FileInfo fileInf = new System.IO.FileInfo(/*pathToScriptFiles + @"\EMSMCconfig.csv"*/ configFilePath);
				if (fileInf.Exists)
				{
					System.IO.StreamReader srConfig = new System.IO.StreamReader(configFilePath, System.Text.Encoding.Default, false);

					string line;
					while ((line = srConfig.ReadLine()) != null)
					{
						string[] scriptParams = line.Split(new char[] { ';' });
						if (scriptParams[0] == "OdbServerName")
							_odbServerName = scriptParams[1];
						/*{	
							if (scriptParams[1] == null || scriptParams[1] == "")
								_odbServerName = "ag-lis-aipim";
							else
								_odbServerName = scriptParams[1];			
						}*/
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

				// �������� ������� ����������� ��� ������� ������
				fileInf = new System.IO.FileInfo(pathToScriptFiles + @"\ExceptionPropertyList.csv");
				if (!fileInf.Exists)
					fileInf.Create();
				fileInf = new System.IO.FileInfo(pathToScriptFiles + @"\ReverseHash.csv");
				if (!fileInf.Exists)
					fileInf.Create();
				fileInf = new System.IO.FileInfo(pathToScriptFiles + @"\ForwardHash.csv");
				if (!fileInf.Exists)
					fileInf.Create();
				fileInf = new System.IO.FileInfo(pathToScriptFiles + @"\�������� ���������.csv");
				if (!fileInf.Exists)
					fileInf.Create();

				// ������ ��� ������ � �������		
				System.IO.StreamWriter swReverse = null;
				System.IO.StreamWriter swForward = null;
				System.IO.StreamWriter swDiff = null;
				System.IO.StreamReader srExcep = null;

				// ������ � ����������� � ������
				List<string> listOfDifferences = new List<string>();
				HashSet<string> exceptionPropertyHash = null;

				ModelImage reverseModelImage;
				ModelImage forwardModelImage;

				//�������� �������� ����, scroll � StackPanel
				Window mainWindow = new Window();
				var scroll = new System.Windows.Controls.ScrollViewer();
				var mainStackPanel = new System.Windows.Controls.StackPanel();

				//������������� �������� ����
				mainWindow.Title = "���� ���������";
				mainWindow.Width = 750;
				mainWindow.MaxWidth = 750;
				mainWindow.Content = scroll;
				mainWindow.ResizeMode = ResizeMode.NoResize;
				mainWindow.SizeToContent = SizeToContent.Height;

				//������������� �������� scroll
				scroll.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Visible;
				scroll.Content = mainStackPanel;

				//���������� ������� ��� ��������� scroll
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

				//=======�Ψ (������)===================================================================

				// GroupBox ��� ������ ����������� � ����
				var grBoxOrganisation = new System.Windows.Controls.GroupBox();
				grBoxOrganisation.IsEnabled = true;
				grBoxOrganisation.Header = "��������� ����������� � ����";
				grBoxOrganisation.Margin = new Thickness(5, 5, 5, 5);

				var orgStackPanel = new System.Windows.Controls.StackPanel();

				// Grid ��� ���������� �����������
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

				// TextBlock ��� ����� ����������� �����������
				var textBlockOrganisation = new System.Windows.Controls.TextBlock();
				textBlockOrganisation.IsEnabled = true;
				textBlockOrganisation.Text = "����������� (UID): ";

				// TextBox ��� ����� ����������� �����������
				var textBoxOrganisation = new System.Windows.Controls.TextBox();
				textBoxOrganisation.IsEnabled = true;
				textBoxOrganisation.Margin = new Thickness(5, 5, 5, 5);

				// Grid ��� ���������� ����
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

				// TextBlock ��� ����� ����������� ���� �����������
				var textBlockOrganisationRole = new System.Windows.Controls.TextBlock();
				textBlockOrganisationRole.IsEnabled = true;
				textBlockOrganisationRole.Text = "���� ����������� (UID): ";

				var ComboBoxOrgRole = new System.Windows.Controls.ComboBox();
				ComboBoxOrgRole.IsEnabled = true;
				ComboBoxOrgRole.Margin = new Thickness(5, 5, 5, 5);
				ComboBoxOrgRole.Items.Add("�������� � ���");
				ComboBoxOrgRole.Items.Add("�������� � ���� (� ������)");
				ComboBoxOrgRole.Items.Add("�������� � ��� (� ������)");
				ComboBoxOrgRole.SelectedIndex = 0;

				mainStackPanel.Children.Add(grBoxOrganisation);

				// ��������� ��������� � organisationGrid
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



				// GroupBox ��� ��������� ���������� ������������ ������
				var grBoxFilesDir = new System.Windows.Controls.GroupBox();
				grBoxFilesDir.IsEnabled = true;
				grBoxFilesDir.Header = "��������� ����� � �������";
				grBoxFilesDir.Margin = new Thickness(5, 5, 5, 5);

				var stackPanelFilesDir = new System.Windows.Controls.StackPanel();

				// Grid ��� ���������� �����
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

				// TextBlock ��� ����� ����� ��� ������ � �������
				var textBlockFilesDir = new System.Windows.Controls.TextBlock();
				textBlockFilesDir.IsEnabled = true;
				textBlockFilesDir.Text = "����� ��� ������ � �������: ";

				// TextBox ��� ����� ����� ��� ������ � �������
				var textBoxDirPath = new System.Windows.Controls.TextBox();
				textBoxDirPath.IsEnabled = true;
				textBoxDirPath.MinWidth = 200;
				textBoxDirPath.Margin = new Thickness(5, 5, 5, 5);

				// TextBlock ��� ����� ����������� �� �����
				var textBlockFilesDirComment = new System.Windows.Controls.TextBlock();
				textBlockFilesDirComment.IsEnabled = true;
				textBlockFilesDirComment.Text = "� ��������� ����� ����� ����������� ��� ���������������� ����� \nExceptionPropertyList.csv, ForwardHash.csv, ReverseHash.csv, \n�������� ��������� �� ���.csv";

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


				// GroupBox ��� ������ ������������ �������
				var grBoxComparedModels = new System.Windows.Controls.GroupBox();
				grBoxComparedModels.IsEnabled = true;
				grBoxComparedModels.Header = "��������� ������������ �������";
				grBoxComparedModels.Margin = new Thickness(5, 5, 5, 5);

				var stackPanelComparedModels = new System.Windows.Controls.StackPanel();

				// Grid ��� �������� �������
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

				// TextBlock ��� ����� ��������� �������� ������
				var textBlockServerName = new System.Windows.Controls.TextBlock();
				textBlockServerName.IsEnabled = true;
				textBlockServerName.Text = "��� �������: ";

				// TextBox ��� ����� ��������� �������� ������
				var textBoxServerName = new System.Windows.Controls.TextBox();
				textBoxServerName.IsEnabled = true;
				textBoxServerName.Width = 200;
				textBoxServerName.Margin = new Thickness(5, 5, 5, 5);
				textBoxServerName.Text = _odbServerName;

				// Grid ��� �������� ��������� �������� ������
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

				// TextBlock ��� ����� ��������� �������� ������
				var textBlockReverseModelContext = new System.Windows.Controls.TextBlock();
				textBlockReverseModelContext.IsEnabled = true;
				textBlockReverseModelContext.Text = "�������� �������� ������: ";

				// TextBox ��� ����� ��������� �������� ������
				var textBoxReverseModelContext = new System.Windows.Controls.TextBox();
				textBoxReverseModelContext.IsEnabled = true;
				textBoxReverseModelContext.Width = 200;
				textBoxReverseModelContext.Margin = new Thickness(5, 5, 5, 5);
				textBoxReverseModelContext.Text = reverseOdbInstanseName;

				// Grid ��� �������� ������ �������� ������
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

				// TextBlock ��� ����� ������ �������� ������
				var textBlockReverseModelNum = new System.Windows.Controls.TextBlock();
				textBlockReverseModelNum.IsEnabled = true;
				textBlockReverseModelNum.Text = "����� �������� ������: ";

				// TextBox ��� ����� ������ �������� ������
				var textBoxReverseModelNum = new System.Windows.Controls.TextBox();
				textBoxReverseModelNum.IsEnabled = true;
				textBoxReverseModelNum.Width = 200;
				textBoxReverseModelNum.Margin = new Thickness(5, 5, 5, 5);
				textBoxReverseModelNum.Text = reverseOdbModelVersionId;

				// Grid ��� �������� ��������� ������������ ������
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

				// TextBlock ��� ����� ��������� ������������ ������
				var textBlockForwardModelContext = new System.Windows.Controls.TextBlock();
				textBlockForwardModelContext.IsEnabled = true;
				textBlockForwardModelContext.Text = "�������� ������������ ������: ";

				// TextBox ��� ����� ��������� ������������ ������
				var textBoxForwardModelContext = new System.Windows.Controls.TextBox();
				textBoxForwardModelContext.IsEnabled = true;
				textBoxForwardModelContext.Width = 200;
				textBoxForwardModelContext.Margin = new Thickness(5, 5, 5, 5);
				textBoxForwardModelContext.Text = forwardOdbInstanseName;

				// Grid ��� �������� ������ ������������ ������
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

				// TextBlock ��� ����� ������ ������������ ������
				var textBlockForwardModelNum = new System.Windows.Controls.TextBlock();
				textBlockForwardModelNum.IsEnabled = true;
				textBlockForwardModelNum.Text = "����� ������������ ������: ";

				// TextBox ��� ����� ������ ������������ ������
				var textBoxForwardModelNum = new System.Windows.Controls.TextBox();
				textBoxForwardModelNum.IsEnabled = true;
				textBoxForwardModelNum.Width = 200;
				textBoxForwardModelNum.Margin = new Thickness(5, 5, 5, 5);
				textBoxForwardModelNum.Text = forwardOdbModelVersionId;

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

				// Button ��� ���������� ��������
				var actionButton = new System.Windows.Controls.Button();
				actionButton.IsEnabled = true;
				actionButton.Height = 30;
				actionButton.Content = "�������� ������";

				mainStackPanel.Children.Add(actionButton);

				// UID ������� �����������
				Guid OrgUid = Services.License.OrganizationUid;
				textBoxOrganisation.Text = OrgUid.ToString();


				// ������ �������
				actionButton.Click += (sender, a) => {
					try
					{
						// ������ ������
						_odbServerName = textBoxServerName.Text;
						reverseOdbInstanseName = textBoxReverseModelContext.Text;
						reverseOdbModelVersionId = textBoxReverseModelNum.Text;
						forwardOdbInstanseName = textBoxForwardModelContext.Text;
						forwardOdbModelVersionId = textBoxForwardModelNum.Text;

						if (_odbServerName == String.Empty || reverseOdbInstanseName == String.Empty || reverseOdbModelVersionId == String.Empty ||
						forwardOdbInstanseName == String.Empty || forwardOdbModelVersionId == String.Empty)
						{
							throw new Exception("������� �� ��� ������ ��� ������ �������");
						}

						// ������ ������ � config ����
						//string configFilePath = pathToScriptFiles + @"\EMSMCconfig.csv";
						System.IO.StreamWriter swConfig = new System.IO.StreamWriter(configFilePath, false, System.Text.Encoding.Default);
						swConfig.WriteLine("OdbServerName;" + _odbServerName);
						swConfig.WriteLine("ReverseOdbInstanseName;" + reverseOdbInstanseName);
						swConfig.WriteLine("ReverseOdbModelVersionId;" + reverseOdbModelVersionId);
						swConfig.WriteLine("ForwardOdbInstanseName;" + forwardOdbInstanseName);
						swConfig.WriteLine("ForwardOdbModelVersionId;" + forwardOdbModelVersionId);
						swConfig.Close();

						// ����������� � �������� ������
						Monitel.Mal.Providers.MalContextParams reverseContext = new Monitel.Mal.Providers.MalContextParams()
						{
							OdbServerName = _odbServerName,
							OdbInstanseName = reverseOdbInstanseName,
							OdbModelVersionId = Convert.ToInt32(reverseOdbModelVersionId),
						};
						Monitel.Mal.Providers.Mal.MalProvider ReverseDataProvider = new Monitel.Mal.Providers.Mal.MalProvider(reverseContext, Monitel.Mal.Providers.MalContextMode.Open, "test", -1);
						reverseModelImage = new ModelImage(ReverseDataProvider, true);

						// ����������� � ������������ ������
						Monitel.Mal.Providers.MalContextParams forwardContext = new Monitel.Mal.Providers.MalContextParams()
						{
							OdbServerName = _odbServerName,
							OdbInstanseName = forwardOdbInstanseName,
							OdbModelVersionId = Convert.ToInt32(forwardOdbModelVersionId),
						};
						Monitel.Mal.Providers.Mal.MalProvider ForwardDataProvider = new Monitel.Mal.Providers.Mal.MalProvider(forwardContext, Monitel.Mal.Providers.MalContextMode.Open, "test", -1);
						forwardModelImage = new ModelImage(ForwardDataProvider, true);

						// ���� � ������ �����
						string reversePath = pathToScriptFiles + @"\ReverseHash.csv";
						System.IO.StreamWriter swReverse = new System.IO.StreamWriter(reversePath, false, System.Text.Encoding.Default);

						// ���� � ������� �����
						string forwardPath = pathToScriptFiles + @"\ForwardHash.csv";
						System.IO.StreamWriter swForward = new System.IO.StreamWriter(forwardPath, false, System.Text.Encoding.Default);

						// ���� � �������� ����� � �����������
						string differencePath = pathToScriptFiles + @"\�������� ���������.csv";
						System.IO.StreamWriter swDiff = new System.IO.StreamWriter(differencePath, false, System.Text.Encoding.Default);

						// ���� � ����� �� ����������, ������������ �� ��������
						string exceptionPropertyListPath = pathToScriptFiles + @"\ExceptionPropertyList.csv";
						System.IO.StreamReader srExcep = new System.IO.StreamReader(exceptionPropertyListPath, System.Text.Encoding.Default, false);

						HashSet<Guid> reverseObjectsUids = new HashSet<Guid>(); // �������� �������� �������� �� ������ ������
						HashSet<Guid> forwardObjectsUids = new HashSet<Guid>(); // �������� �������� �������� �� ����� ������



						exceptionPropertyHash = new HashSet<string>(); // �������� ������, ������� ����������� �� ��������

						// ����� ���� �����������, �� ������� ����������� ��������� �������
						Guid roleTypeUid;
						if (ComboBoxOrgRole.SelectedIndex == 0)
						{
							roleTypeUid = new Guid("1000161E-0000-0000-C000-0000006D746C"); // �������� � ���
							ODUSVCreatingListOfComparedObjectsMTN(reverseObjectsUids, ODUSVFindOrganisationRole(roleTypeUid, OrgUid), reverseModelImage);
							ODUSVCreatingListOfComparedObjectsMTN(forwardObjectsUids, ODUSVFindOrganisationRole(roleTypeUid, OrgUid), forwardModelImage);
						}
						else if (ComboBoxOrgRole.SelectedIndex == 1)
						{
							roleTypeUid = new Guid("10001672-0000-0000-C000-0000006D746C"); // �������� � ����
							MessageBox.Show("�������� ��������� �� ��������, �������������� � ����, ��� �� �����������");
						}
						else if (ComboBoxOrgRole.SelectedIndex == 2)
						{
							roleTypeUid = new Guid("10001669-0000-0000-C000-0000006D746C"); // �������� � ���
							MessageBox.Show("�������� ��������� �� ��������, �������������� � ���, ��� �� �����������");
						}

						// ������ ������ �������� �� ����������� ������� � ��������������� �����
						foreach (Guid uid in reverseObjectsUids)
						{
							swReverse.WriteLine(uid);
						}
						foreach (Guid uid in forwardObjectsUids)
						{
							swForward.WriteLine(uid);
						}

						// ����������� ������ �� �������, ������������ �� ��������
						string line;
						while ((line = srExcep.ReadLine()) != null)
						{
							exceptionPropertyHash.Add(line);
						}

						//������� ��������� ���� ��� ���������� ������
						swReverse.Close();
						swForward.Close();
						srExcep.Close();


						MessageBox.Show("� ������ ����������: " + exceptionPropertyHash.Count());
						MessageBox.Show("��������� reverse ��������: " + reverseObjectsUids.Count());
						MessageBox.Show("��������� forward ��������: " + forwardObjectsUids.Count());

						// ����� ������������ ������ ��������� � �������
						listOfDifferences.Add("UID;��������;�����;��� �������;����� �������;������������� �� �������������");   // ����� �������

						HashSet<Guid> deletedObjectsUids = new HashSet<Guid>(reverseObjectsUids);   // �������� �������� ��������� �� ������
						deletedObjectsUids.ExceptWith(forwardObjectsUids);

						MessageBox.Show("��������� ��������: " + deletedObjectsUids.Count());

						foreach (Guid uid in deletedObjectsUids)
						{
							ODUSVWriteAddedOrDeletedObject(uid, reverseModelImage, "deleted");
						}

						HashSet<Guid> addedObjectsUids = new HashSet<Guid>(forwardObjectsUids); // �������� �������� ����������� � ������
						addedObjectsUids.ExceptWith(reverseObjectsUids);

						MessageBox.Show("����������� ��������: " + addedObjectsUids.Count());

						foreach (Guid uid in addedObjectsUids)
						{
							ODUSVWriteAddedOrDeletedObject(uid, forwardModelImage, "added");
						}

						// ���������� � ������ ���������� ��������
						HashSet<Guid> otherMTNUids = new HashSet<Guid>(reverseObjectsUids); // �������� �������� ����������� � ������
						otherMTNUids.IntersectWith(forwardObjectsUids);

						MessageBox.Show("���������� ��������: " + otherMTNUids.Count());

						// ����������� ������ ���������
						foreach (Guid uid in otherMTNUids)
						{
							ODUSVFindChanges(uid);
						}


						// ���������� ����� � ����������
						foreach (string text in listOfDifferences)
						{
							swDiff.WriteLine(text);
						}

						swDiff.Close();
						MessageBox.Show("������ �������� �������");
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



				//===================������===============================================

				Guid ODUSVFindOrganisationRole(Guid roleTypeUid, Guid organisationUid)
				{
					Guid result = new Guid();

					var organisation = ModelImage.GetObject<Organisation>(organisationUid);
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

				// ����� �������� ������ � UID'��� ��������, ����������� � ���
				void ODUSVCreatingListOfComparedObjectsMTN(HashSet<Guid> hashOfObjects, /*OrganisationRole orgRole*/ Guid Uid, ModelImage mImg)
				{
					var mObjects = mImg.GetObjects<IdentifiedObject>();
					var orgRole = mImg.GetObject<OrganisationRole>(Uid);

					foreach (IdentifiedObject obj in orgRole.Objects)
					{
						// ��������� �������������� ������������
						hashOfObjects.Add(obj.Uid);

						// ��������� ��� �������� �������
						ODUSVCreatingListOfChildObjects(obj, hashOfObjects);
					}
				}

				// ����������� ����� ���������� � ������ �������� ��������
				void ODUSVCreatingListOfChildObjects(IdentifiedObject currentObj, HashSet<Guid> hashOfChildObjects)
				{
					if (currentObj.ChildObjects.Count() > 0)
					{
						foreach (IdentifiedObject childObj in currentObj.ChildObjects)
						{
							// ��������� � ������ ������������
							hashOfChildObjects.Add(childObj.Uid);

							// ��������� � ��������� � ������ ��� ��������� ������������ � �� �������� �������
							ODUSVFindWeatherStations(childObj, hashOfChildObjects);

							// ��������� � ��������� � ������ ��� ��������� ������� �������� ���
							ODUSVFindPEStageSetPoints(childObj, hashOfChildObjects);

							// ���������� ��������� � ������ ��� �������� �������
							ODUSVCreatingListOfChildObjects(childObj, hashOfChildObjects);
						}
					}
				}

				// ����� ���������� � ������ ��������� � ������������� ������������
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

				// ����� ���������� � ������ ������� �������� ���
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

				// ����� ������ ������������/���������� ������� �� �� UID � ����� ������ ���������
				void ODUSVWriteAddedOrDeletedObject(Guid uid, ModelImage mImg, string state)
				{
					var selectedObject = mImg.GetObject<IdentifiedObject>(uid);
					switch (state)
					{
						case "deleted":
							listOfDifferences.Add(Convert.ToString(selectedObject.Uid) + ";��������;������ ������ ������ " + selectedObject.ClassName() + ";" + selectedObject.name + ";" + selectedObject.ClassName());
							break;
						case "added":
							listOfDifferences.Add(Convert.ToString(selectedObject.Uid) + ";����������;�������� ������ ������ " + selectedObject.ClassName() + ";" + selectedObject.name + ";" + selectedObject.ClassName());
							break;
					}
				}


				void ODUSVFindChanges(Guid uid)
				{
					var reverseObject = reverseModelImage.GetObject</*IdentifiedObject*/IMalObject>(uid);
					// �������� �� �������������� � ��������� �������� ��� �������� ���
					if (reverseObject != null)
					{
						var forwardObject = forwardModelImage.GetObject</*IdentifiedObject*/IMalObject>(uid);

						// ���� ����������� ������ - IMalObject
						if (reverseObject is IMalObject)
						{
							Type t = reverseObject.GetType();


							//(int)Convert.ChangeType(flt, typeof(int));
							var iMORev = reverseObject as IMalObject;
							var iMOForw = forwardObject as IMalObject;

							ODUSVPropertyReflectInfo(iMORev, iMOForw, exceptionPropertyHash, listOfDifferences);
						}

						#region             
						// ���� ����������� ������ - ������� ����� ����������� ����
						if (reverseObject is ACLineSegment)
						{
							ACLineSegment rObj = reverseObject as ACLineSegment;
							ACLineSegment fObj = forwardObject as ACLineSegment;
							ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
						}

						// ���� ����������� ������ - ������
						else if (reverseObject is Analog)
						{
							Analog rObj = reverseObject as Analog;
							Analog fObj = forwardObject as Analog;
							ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
						}

						// ���� ����������� ������ - �������
						else if (reverseObject is Discrete)
						{
							Discrete rObj = reverseObject as Discrete;
							Discrete fObj = forwardObject as Discrete;
							ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
						}

						// ���� ����������� ������ - ��������
						else if (reverseObject is BusArrangement)
						{
							BusArrangement rObj = reverseObject as BusArrangement;
							BusArrangement fObj = forwardObject as BusArrangement;
							ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
						}

						// ���� ����������� ������ - ����� ���������������� �����������		
						else if (reverseObject is OperationalLimitSet)
						{
							OperationalLimitSet rObj = reverseObject as OperationalLimitSet;
							OperationalLimitSet fObj = forwardObject as OperationalLimitSet;
							ODUSVPropertyReflectInfo(rObj, fObj, exceptionPropertyHash, listOfDifferences);
						}

						// ���� ����������� ������ - ������ ����		
						else if (reverseObject is CurrentLimit)
						{
							CurrentLimit cLRev = reverseObject as CurrentLimit;
							CurrentLimit cLForw = forwardObject as CurrentLimit;

							ODUSVPropertyReflectInfo(cLRev, cLForw, exceptionPropertyHash, listOfDifferences);
						}

						//CurrentFlowUnbalanceLimit
						// ���� ����������� ������ - ������ ����������� ����� ���
						else if (reverseObject is CurrentFlowUnbalanceLimit)
						{
							CurrentFlowUnbalanceLimit cFULRev = reverseObject as CurrentFlowUnbalanceLimit;
							CurrentFlowUnbalanceLimit cFULForw = forwardObject as CurrentFlowUnbalanceLimit;

							ODUSVPropertyReflectInfo(cFULRev, cFULForw, exceptionPropertyHash, listOfDifferences);
						}

						// ���� ����������� ������ - ������ ����������� ���� �� �����������
						else if (reverseObject is CurrentVsTemperatureLimitCurve)
						{
							CurrentVsTemperatureLimitCurve cVTLCRev = reverseObject as CurrentVsTemperatureLimitCurve;
							CurrentVsTemperatureLimitCurve cVTLCForw = forwardObject as CurrentVsTemperatureLimitCurve;

							ODUSVPropertyReflectInfo(cVTLCRev, cVTLCForw, exceptionPropertyHash, listOfDifferences);
						}

						//PotentialTransformer
						// ���� ����������� ������ - ������������� ����������
						else if (reverseObject is PotentialTransformer)
						{
							PotentialTransformer pTRev = reverseObject as PotentialTransformer;
							PotentialTransformer pTForw = forwardObject as PotentialTransformer;

							ODUSVPropertyReflectInfo(pTRev, pTForw, exceptionPropertyHash, listOfDifferences);
						}

						// PotentialTransformerWinding
						// ���� ����������� ������ - ������� �������������� ����������
						else if (reverseObject is PotentialTransformerWinding)
						{
							PotentialTransformerWinding pTWRev = reverseObject as PotentialTransformerWinding;
							PotentialTransformerWinding pTWForw = forwardObject as PotentialTransformerWinding;

							ODUSVPropertyReflectInfo(pTWRev, pTWForw, exceptionPropertyHash, listOfDifferences);
						}

						// Terminal
						// ���� ����������� ������ - ����� ������������
						else if (reverseObject is Terminal)
						{
							Terminal tRev = reverseObject as Terminal;
							Terminal tForw = forwardObject as Terminal;

							ODUSVPropertyReflectInfo(tRev, tForw, exceptionPropertyHash, listOfDifferences);
						}

						// WaveTrap
						// ���� ����������� ������ - ��� �����������
						else if (reverseObject is WaveTrap)
						{
							WaveTrap wTRev = reverseObject as WaveTrap;
							WaveTrap wTForw = forwardObject as WaveTrap;

							ODUSVPropertyReflectInfo(wTRev, wTForw, exceptionPropertyHash, listOfDifferences);
						}

						// Breaker
						// ���� ����������� ������ - �����������
						else if (reverseObject is Breaker)
						{
							Breaker bRev = reverseObject as Breaker;
							Breaker bForw = forwardObject as Breaker;

							ODUSVPropertyReflectInfo(bRev, bForw, exceptionPropertyHash, listOfDifferences);
						}

						// AnalogValue
						// ���� ����������� ������ - ���������� ��������
						else if (reverseObject is AnalogValue)
						{
							AnalogValue aVRev = reverseObject as AnalogValue;
							AnalogValue aVForw = forwardObject as AnalogValue;

							ODUSVPropertyReflectInfo(aVRev, aVForw, exceptionPropertyHash, listOfDifferences);
						}

						// DiscreteValue
						// ���� ����������� ������ - ���������� ��������
						else if (reverseObject is DiscreteValue)
						{
							DiscreteValue dVRev = reverseObject as DiscreteValue;
							DiscreteValue dVForw = forwardObject as DiscreteValue;

							ODUSVPropertyReflectInfo(dVRev, dVForw, exceptionPropertyHash, listOfDifferences);
						}

						// CurrentTransformer
						// ���� ����������� ������ - ������������� ����
						else if (reverseObject is CurrentTransformer)
						{
							CurrentTransformer cTRev = reverseObject as CurrentTransformer;
							CurrentTransformer cTForw = forwardObject as CurrentTransformer;

							ODUSVPropertyReflectInfo(cTRev, cTForw, exceptionPropertyHash, listOfDifferences);
						}

						// CurrentTransformerWinding
						// ���� ����������� ������ - ������� �������������� ����
						else if (reverseObject is CurrentTransformerWinding)
						{
							CurrentTransformerWinding cTWRev = reverseObject as CurrentTransformerWinding;
							CurrentTransformerWinding cTWForw = forwardObject as CurrentTransformerWinding;

							ODUSVPropertyReflectInfo(cTWRev, cTWForw, exceptionPropertyHash, listOfDifferences);
						}

						// Disconnector
						// ���� ����������� ������ - �������������
						else if (reverseObject is Disconnector)
						{
							Disconnector dRev = reverseObject as Disconnector;
							Disconnector dForw = forwardObject as Disconnector;

							ODUSVPropertyReflectInfo(dRev, dForw, exceptionPropertyHash, listOfDifferences);
						}

						// Line
						// ���� ����������� ������ - ���
						else if (reverseObject is Line)
						{
							Line lRev = reverseObject as Line;
							Line lForw = forwardObject as Line;

							ODUSVPropertyReflectInfo(lRev, lForw, exceptionPropertyHash, listOfDifferences);
						}

						// PowerTransformer
						// ���� ����������� ������ - ������� �������������
						else if (reverseObject is PowerTransformer)
						{
							PowerTransformer pTRev = reverseObject as PowerTransformer;
							PowerTransformer pTForw = forwardObject as PowerTransformer;

							ODUSVPropertyReflectInfo(pTRev, pTForw, exceptionPropertyHash, listOfDifferences);
						}

						// PowerTransformerEnd
						// ���� ����������� ������ - ������� ��������������
						else if (reverseObject is PowerTransformerEnd)
						{
							PowerTransformerEnd pTERev = reverseObject as PowerTransformerEnd;
							PowerTransformerEnd pTEForw = forwardObject as PowerTransformerEnd;

							ODUSVPropertyReflectInfo(pTERev, pTEForw, exceptionPropertyHash, listOfDifferences);
						}

						// CurrentVsTapStepLimitCurve
						// ���� ����������� ������ - ������ ����������� ���� �� ��������������� �����������
						else if (reverseObject is CurrentVsTapStepLimitCurve)
						{
							CurrentVsTapStepLimitCurve cVTSLCRev = reverseObject as CurrentVsTapStepLimitCurve;
							CurrentVsTapStepLimitCurve cVTSLCForw = forwardObject as CurrentVsTapStepLimitCurve;

							ODUSVPropertyReflectInfo(cVTSLCRev, cVTSLCForw, exceptionPropertyHash, listOfDifferences);
						}

						// RatioTapChanger
						// ���� ����������� ������ - ���/���
						else if (reverseObject is RatioTapChanger)
						{
							RatioTapChanger rTCRev = reverseObject as RatioTapChanger;
							RatioTapChanger rTCForw = forwardObject as RatioTapChanger;

							ODUSVPropertyReflectInfo(rTCRev, rTCForw, exceptionPropertyHash, listOfDifferences);
						}

						// TransformerMeshImpedance
						// ���� ����������� ������ - ���������������� ����� ��������������
						else if (reverseObject is TransformerMeshImpedance)
						{
							TransformerMeshImpedance tMIRev = reverseObject as TransformerMeshImpedance;
							TransformerMeshImpedance tMIForw = forwardObject as TransformerMeshImpedance;

							ODUSVPropertyReflectInfo(tMIRev, tMIForw, exceptionPropertyHash, listOfDifferences);
						}

						// LoadSheddingEquipment
						// ���� ����������� ������ - ���������� ����������� �����������
						else if (reverseObject is LoadSheddingEquipment)
						{
							LoadSheddingEquipment lSERev = reverseObject as LoadSheddingEquipment;
							LoadSheddingEquipment lSEForw = forwardObject as LoadSheddingEquipment;

							ODUSVPropertyReflectInfo(lSERev, lSEForw, exceptionPropertyHash, listOfDifferences);
						}

						// GenericPSR
						// ���� ����������� ������ - ������ ������������
						else if (reverseObject is GenericPSR)
						{
							GenericPSR gPSRRev = reverseObject as GenericPSR;
							GenericPSR gPSRForw = forwardObject as GenericPSR;

							ODUSVPropertyReflectInfo(gPSRRev, gPSRForw, exceptionPropertyHash, listOfDifferences);
						}

						// GenerationUnloadingStage
						// ���� ����������� ������ - ������� ��
						else if (reverseObject is GenerationUnloadingStage)
						{
							GenerationUnloadingStage gUSRev = reverseObject as GenerationUnloadingStage;
							GenerationUnloadingStage gUSForw = forwardObject as GenerationUnloadingStage;

							ODUSVPropertyReflectInfo(gUSRev, gUSForw, exceptionPropertyHash, listOfDifferences);
						}

						// LoadSheddingStage
						// ���� ����������� ������ - ������� ��
						else if (reverseObject is LoadSheddingStage)
						{
							LoadSheddingStage lSSRev = reverseObject as LoadSheddingStage;
							LoadSheddingStage lSSForw = forwardObject as LoadSheddingStage;

							ODUSVPropertyReflectInfo(lSSRev, lSSForw, exceptionPropertyHash, listOfDifferences);
						}

						// LimitExpression
						// ���� ����������� ������ - ��������� ����������������� �����������
						else if (reverseObject is LimitExpression)
						{
							LimitExpression lERev = reverseObject as LimitExpression;
							LimitExpression lEForw = forwardObject as LimitExpression;

							ODUSVPropertyReflectInfo(lERev, lEForw, exceptionPropertyHash, listOfDifferences);
						}

						// PSRMeasOperand
						// ���� ����������� ������ - ������� ��������� �������������
						else if (reverseObject is PSRMeasOperand)
						{
							PSRMeasOperand pSRMORev = reverseObject as PSRMeasOperand;
							PSRMeasOperand pSRMOForw = forwardObject as PSRMeasOperand;

							ODUSVPropertyReflectInfo(pSRMORev, pSRMOForw, exceptionPropertyHash, listOfDifferences);
						}

						// PEStageSetpoint
						// ���� ����������� ������ - ������� ������� ���
						else if (reverseObject is PEStageSetpoint)
						{
							PEStageSetpoint pESSRev = reverseObject as PEStageSetpoint;
							PEStageSetpoint pESSForw = forwardObject as PEStageSetpoint;

							ODUSVPropertyReflectInfo(pESSRev, pESSForw, exceptionPropertyHash, listOfDifferences);
						}

						// WeatherStation
						// ���� ����������� ������ - ������������
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
							// ���� ����������� ������ - ������� ������� ���
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

				// ��������. ����� ���������� ��� �������� ������
				void ODUSVPropertyReflectInfo<T>(T objRev, T objForw, HashSet<string> exHash, List<string> listOfDiffs) where T : class
				{
					// � ���������� �������� ��� ���������� �������
					Type t = typeof(T);

					// � ���������� ��������� ��� ����������� ��������� ������� ����������
					var interfaces = t.GetInterfaces();
					// ����������� ������ �� ���� �����������
					foreach (Type interf in interfaces)
					{
						// ���������� ��������� �������� ��� ����������, ����� IMalObject
						if (interf.Name != "IMalObject")
						{
							// ����������� ������ �� ���� ��������� ���������� ����������
							foreach (System.Reflection.PropertyInfo prop in interf.GetProperties())
							{
								ODUSVCheckingIdentityOfProperty(exHash, listOfDiffs, objRev, objForw, prop);
							}
						}

					}

					// � ���������� ��������� ��� ��������� � ���������� ������ �������� (������
					// ��, ��� ������� � ����� ������, � �� �����������)
					System.Reflection.PropertyInfo[] propNames = t.GetProperties();

					// ����������� ������ �� ���� ��������� ���������� ������
					foreach (System.Reflection.PropertyInfo prop in propNames)
					{
						ODUSVCheckingIdentityOfProperty(exHash, listOfDiffs, objRev, objForw, prop);
					}
				}

				// ����� �������� ������������ �������� � ���� �������� ��
				void ODUSVCheckingIdentityOfProperty<T>(HashSet<string> _exHash, List<string> _listOfDiffs, T _objRev, T _objForw, System.Reflection.PropertyInfo _prop) where T : class
				{
					try
					{
						// � ���������� �������� ��� ���������� �������
						Type t = typeof(T);

						// �������� ��������� ���������� �������� � ������ ����������
						if (_exHash.Contains(_prop.Name) != true)
						{
							// � ���������� �������� �������� ������������ �������� ���������� �������
							var vals = _prop.GetValue(_objRev);
							var valsForw = _prop.GetValue(_objForw);

							// ���� ��������� �������� �������� ������ ������, �� ����������� ��������� �������
							if (vals is Array)
							{
								// ���� ��������� �������� - ��� ������ ���� System.Byte[], 
								// �� ����������� �������� ������� �� �������������� � ������ Curve
								if (_prop.PropertyType.ToString() == "System.Byte[]" && _objRev is Curve)
								{
									// �������� ����� ������ ������ � ����� �������
									var curvePointsRev = (_objRev as Curve).GetCurvePoints();
									var curvePointsForw = (_objForw as Curve).GetCurvePoints();

									string stringOfPointsRev = String.Empty;
									string stringOfPointsForw = String.Empty;

									// �� �������� � ��������� ����� ���� �� ������ ������� �� ��������� ����� �������� ������ � ���������� ��
									for (int i = 0; i < curvePointsRev.xvalue.Count(); i++)
									{ stringOfPointsRev = stringOfPointsRev + curvePointsRev.xvalue[i] + curvePointsRev.y1value[i]; }
									for (int i = 0; i < curvePointsForw.xvalue.Count(); i++)
									{ stringOfPointsForw = stringOfPointsForw + curvePointsForw.xvalue[i] + curvePointsForw.y1value[i]; }

									if (stringOfPointsRev != stringOfPointsForw)
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
												"' ���������� �������� � �������� '" + _prop.Name + "'. ���� " + "points1" + ", ����� " +
												"points2" + ";" + ODUSVGetObjectName(_objRev) + ";" +
												(_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));
								}

								else if (_prop.PropertyType.ToString() == "Monitel.Mal.Context.CIM16.BranchGroupTerminal[]" && _objRev is Terminal)
								{
									// ���������� � ��������� �����
									// ���������� UID'�� �������� � ������������� ������
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

									// ����������� ����� ��������������� � ������� �� ����������� ��������
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
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
											"' ���������� ����� " + _prop.Name + ";" + ODUSVGetObjectName(_objRev) + ";" +
											(_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));
								}

								else
								{
									// �������� �������� � ����� ������� �� null
									if (_prop.GetValue(_objRev) == null && _prop.GetValue(_objForw) != null)
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
										"' ���������� ����� " + _prop.Name + ";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() +
										";" + ODUSVGetModelingAuthoritySet(_objRev));

									if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) == null)
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
										"' ���������� ����� " + _prop.Name + ";" + ODUSVGetObjectName(_objRev) + ";" +
										(_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));

									if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) != null)
									{
										// ���������� UID'�� �������� � ������������� ������
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

										// ����������� ����� ��������������� � ������� �� ����������� ��������
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
											_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
												"' ���������� ����� " + _prop.Name + ";" + ODUSVGetObjectName(_objRev) + ";" +
												(_objRev as IdentifiedObject).ClassName() + ";" + ODUSVGetModelingAuthoritySet(_objRev));
									}
								}
							}
							// ���� ��������� �������� - ��� �� ������ ������
							else
							{
								// ���� ��������� �������� ���������, �������� ��� ������
								if (_prop.PropertyType.ToString() == "System.String" || _prop.PropertyType.ToString() == "System.Boolean" ||
											_prop.PropertyType.ToString() == "System.Nullable`1[System.Double]" || _prop.PropertyType.ToString() == "System.Double"
											|| _prop.PropertyType.ToString() == "System.Int32" || _prop.PropertyType.ToString() == "System.Int64")
								{

									// �������� �������� � ����� ������� �� null
									if (_prop.GetValue(_objRev) == null && _prop.GetValue(_objForw) != null)
									{
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
										"' ���������� �������� � �������� '" + _prop.Name + "'. ���� '�� �����'" + ", ����� " + _prop.GetValue(_objForw).ToString().Replace("\r\n", "").Replace("\n", "") +
										";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() + ";" +
										ODUSVGetModelingAuthoritySet(_objRev));
									}
									else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) == null)
									{
										if ((_objRev as IdentifiedObject).Uid == new Guid("A8DFF2C0-68F3-4F97-B0AE-58ABAE838B00")) MessageBox.Show(_prop.Name + " = " + _prop.GetValue(_objRev));
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
										"' ���������� �������� � �������� '" + _prop.Name + "'. ���� " + _prop.GetValue(_objRev).ToString().Replace("\r\n", "").Replace("\n", "") + ", ����� '�� �����'" +
										";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() + ";" +
										ODUSVGetModelingAuthoritySet(_objRev));
									}
									else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) != null)
									{
										if (_prop.GetValue(_objRev).ToString() != _prop.GetValue(_objForw).ToString())
										{
											if (_objRev is IdentifiedObject && _objForw is IdentifiedObject)
												_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
												"' ���������� �������� � �������� '" + _prop.Name + "'. ���� " + _prop.GetValue(_objRev).ToString().Replace("\r\n", "").Replace("\n", "") + ", ����� " +
												_prop.GetValue(_objForw).ToString().Replace("\r\n", "").Replace("\n", "") + ";" + (_objRev as IdentifiedObject).name + ";" + (_objRev as IdentifiedObject).ClassName() +
												";" + ODUSVGetModelingAuthoritySet(_objRev));
											else
												_listOfDiffs.Add(Convert.ToString((_objRev as IMalObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
												"' ���������� �������� � �������� '" + _prop.Name + "'. ���� " + _prop.GetValue(_objRev).ToString().Replace("\r\n", "").Replace("\n", "") + ", ����� " +
												_prop.GetValue(_objForw).ToString().Replace("\r\n", "").Replace("\n", "") + ";" + (_objRev as IMalObject).ClassName()/*name*/ + ";" + (_objRev as IMalObject).ClassName() +
												";" + ODUSVGetModelingAuthoritySet(_objRev));
										}
									}
								}

								// ���� ��������� �������� - ��� ������ ������-�� ������
								else
								{
									// �������� �������� � ����� ������� �� null
									if (_prop.GetValue(_objRev) == null && _prop.GetValue(_objForw) != null)
									{
										string line = "";
										// �������� �� �����
										if (valsForw is Terminal)
										{
											line = ODUSVGetObjectName(valsForw);

										}
										else if (valsForw is IdentifiedObject)
										{
											line = ODUSVGetObjectName(valsForw);
										}
										else line = ODUSVGetObjectName(valsForw);

										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
										"' ���������� �������� � �������� '" + _prop.Name + "'. ���� '�� �����'" + ", ����� " + line +
										";" + ODUSVGetObjectName(_objRev) + ";" + (_objRev as IdentifiedObject).ClassName() + ";" +
										ODUSVGetModelingAuthoritySet(_objRev));
									}
									else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) == null)
									{
										string line = "";
										// �������� �� �����
										if (vals is Terminal)
											line = ODUSVGetObjectName(vals);
										else if (vals is IdentifiedObject)
											line = ODUSVGetObjectName(vals);
										else line = ODUSVGetObjectName(vals);
										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
											"' ���������� �������� � �������� '" + _prop.Name + "'. ���� " + line + ", ����� '�� �����'" +
											";" + ODUSVGetObjectName(_objRev) + ";" + (_objRev as IdentifiedObject).ClassName() + ";"
											+ ODUSVGetModelingAuthoritySet(_objRev));
									}
									else if (_prop.GetValue(_objRev) != null && _prop.GetValue(_objForw) != null &&
												_prop.GetValue(_objRev).ToString() != _prop.GetValue(_objForw).ToString())
									{
										string line1 = "";
										string line2 = "";
										// �������� �� �����
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

										_listOfDiffs.Add(Convert.ToString((_objRev as IdentifiedObject).Uid) + ";���������;� ������� ������ '" + t.ToString() +
												"' ���������� �������� � �������� '" + _prop.Name + "'. ���� " + line1 + ", ����� " +
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



				// ����� ����������� ������ ������� ��
				string ODUSVGetObjectName<T>(/*IMalObject*/T inputObject) where T : class
				{
					string result = String.Empty;
					if (inputObject is IMalObject)
					{
						// ������������� ������ ��� ���������� ������
						IMalObject obj = inputObject as IMalObject;

						// ���� ������ �������� ��������� ��������, �� �� ������� � �� ������ �����������
						if ((obj is IdentifiedObject) == true && (obj is Terminal) == false && (obj is ConnectivityNode) == false)
						{
							result = (obj as IdentifiedObject).name;
						}
						// ���� ������ �������� �������
						else if (obj is Terminal)
						{
							result = "T" + (obj as Terminal).sequenceNumber + " " + (obj as Terminal).ConductingEquipment.name;
						}
						// ���� ������ �������� ��������� ��������, �� �� ������� � �� ������ �����������
						else if (obj is ConnectivityNode)
						{
							result = "ConnectivityNode : " + obj.Id + " " + obj.Uid;
						}
						// ���� ������ �� �������� ���������
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


				//===========�Ψ (�����)===============================================================



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
				scriptWindow.Caption = "��������� ������� EMS";
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
