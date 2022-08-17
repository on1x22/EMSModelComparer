using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMSModelComparer
{
    internal class EMSController : IController
    {
        IModel model;
        MainWindow mainWindow;

        internal EMSController(Window MainWindow, IServiceManager Services, IModel model)
        {
            this.model = model;

            mainWindow = new MainWindow(MainWindow, Services, model, this);
            mainWindow.InitializeWindow();
            mainWindow.Start();
        }

        public void StartComparison()
        {
            SetModelConfiguration();
            model.CompareModels();
        }

        private void SetModelConfiguration()
        {
            model.OdbServerName = mainWindow.TextBoxServerName;
            model.ReverseOdbInstanseName = mainWindow.TextBoxReverseModelContext;
            model.ReverseOdbModelVersionId = mainWindow.TextBoxReverseModelNum;
            model.ForwardOdbInstanseName = mainWindow.TextBoxForwardModelContext;
            model.ForwardOdbModelVersionId = mainWindow.TextBoxForwardModelNum;
            model.OrganisationRoleIndex = mainWindow.ComboBoxOrgRoleIndex;
            model.OrganisationUID = new Guid(mainWindow.TextBoxOrganisation);
        }
    }
}
