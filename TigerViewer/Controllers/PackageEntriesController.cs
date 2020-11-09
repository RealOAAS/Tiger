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
    public class PackageEntriesController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<Tiger.Formats.Entry> Get()
        {
            string package_name = Request.Headers["package_name"];
            return TigerIntegration.extractor.package(package_name).entry_table();
        }
    }
}
