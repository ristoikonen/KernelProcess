using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKProcess.Plugins
{

    /// <summary>
    /// A plugin that returns old accounts information of the user.
    /// builder.Plugins.AddFromType<AccountHistory>("Account history")
    /// </summary>
    internal class AccountHistory
    {
        [KernelFunction]
        [Description("Returns account type for the user.")]
        public string GetOldAccount([Description("Email address of the user.")] string email)
        {
            return email.Equals("axelf@gmx.com", StringComparison.OrdinalIgnoreCase) ? "Advanced Account" : "Basic Account";
        }

        [KernelFunction]
        [Description("Returns accounts creadit limit.")]
        public string GetOldAccountCreaditLimit([Description("Email address of the user.")] string email)
        {
            return email.Equals("axelf@gmx.com", StringComparison.OrdinalIgnoreCase) ? "$20000" : "$5000";
        }
    }
}
