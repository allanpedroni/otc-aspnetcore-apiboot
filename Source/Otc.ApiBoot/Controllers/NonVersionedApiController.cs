using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Otc.ApiBoot.Controllers
{
    /// <summary>
    /// Controller base for api controllers without any route configured.
    /// It requires authorization, if you need to disable authorization on 
    /// inherited controller, add AllowAnonoymous decoration.
    /// </summary>
    [ApiController, Authorize]
    public abstract class NonVersionedApiController : ControllerBase
    {
    }
}
