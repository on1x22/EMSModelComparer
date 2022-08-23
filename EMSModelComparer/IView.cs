using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMSModelComparer
{
    internal interface IView
    {
        string ServerName { get; }
        string ReverseModelContext { get; }
        string ReverseModelId { get; }
        string ForwardModelContext { get; }
        string ForwardModelId { get; }
        string OrganisationUid { get; }
        int OrganisationRoleIndex { get; }
        bool IsFileOpen { get; }
        //void InitializeWindow();        
    }
}
