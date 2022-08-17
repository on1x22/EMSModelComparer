using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMSModelComparer
{
    internal interface IModel
    {
        string OdbServerName { get; set; }
        string ReverseOdbInstanseName { get; set; }
        string ReverseOdbModelVersionId { get; set; }
        string ForwardOdbInstanseName { get; set; }
        string ForwardOdbModelVersionId { get; set; }

        Guid OrganisationUID { get; set; }

        int OrganisationRoleIndex { get; set; }

        void CompareModels();
    }
}
