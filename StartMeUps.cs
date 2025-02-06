using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FxConsole
{
    public interface IStartMeUps
    {
        public Uri ModelEndpoint { get; set; }
        public string ModelName { get; set; }
    }
    public class StartMeUps
    {
        public required Uri ModelEndpoint { get; set; }
        public required string ModelName { get; set; }
    }
}
