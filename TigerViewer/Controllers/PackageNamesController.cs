using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TigerViewer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PackageNamesController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return TigerIntegration.extractor.MasterPackageNames;
        }
    }
}
