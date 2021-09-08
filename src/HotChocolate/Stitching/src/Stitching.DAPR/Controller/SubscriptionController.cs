using System.Threading.Tasks;
using Dapr;
using Microsoft.AspNetCore.Mvc;

namespace HotChocolate.Stitching.DAPR.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class SubscriptionController : ControllerBase
    {     
        [HttpGet]
        [Topic(DaprConfiguration.PubSubComponent, DaprConfiguration.PubSubTopic)]

        public async Task<IActionResult> StitchingSubscription(RemoteSchemaDefinition schemaDefinition)
        {

            return Ok();
        }
    }
}
