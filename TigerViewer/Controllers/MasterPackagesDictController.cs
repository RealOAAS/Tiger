using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft;

namespace TigerViewer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MasterPackagesDictController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(TigerIntegration.extractor.generate_master_packages_dict());
        }
    }
}
