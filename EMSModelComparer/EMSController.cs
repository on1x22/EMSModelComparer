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

        internal EMSController(IModel model)
        {
            this.model = model;

            mainWindow = new MainWindow(model, this);
            mainWindow.ShowDialog();
        }

        public void StartComparison()
        {
            SetModelConfiguration();
            model.CompareModels();
        }

        private void SetModelConfiguration()
        {
            try
            {
                model.OdbServerName = mainWindow.ServerName;
                model.ReverseOdbInstanseName = mainWindow.ReverseModelContext;
                model.ReverseOdbModelVersionId = mainWindow.ReverseModelId;
                model.ForwardOdbInstanseName = mainWindow.ForwardModelContext;
                model.ForwardOdbModelVersionId = mainWindow.ForwardModelId;
                model.OrganisationRoleIndex = mainWindow.OrganisationRoleIndex;
                model.OrganisationUid = new Guid(mainWindow.OrganisationUid);
                model.IsFileOpen = mainWindow.IsFileOpen;
            }
            catch
            { 
                throw new Exception("Введены некорректные данные");
            }
        }
    }
}
