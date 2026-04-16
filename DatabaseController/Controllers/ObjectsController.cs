using DatabaseController.Channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DatabaseController.Controllers
{
    [ApiController]
    [Route("api/objects")]
    public class ObjectsController : ControllerBase
    {
        private readonly ObjectQueue _queue;
        private readonly Data.AppDbContext _db;

        public ObjectsController(ObjectQueue queue, Data.AppDbContext db)
        {
            _queue = queue;
            _db = db;
        }

        [HttpPost("addMany")]
        public async Task<IActionResult> AddManyObjects([FromBody] List<DTOs.AddObjectRequest> obj)
        {
            if (obj == null || obj.Count == 0)
                return BadRequest(new { message = "Empty request body" });

            try
            {
                foreach (var o in obj)
                {
                    var model = new Models.ModelObject
                    {
                        Guid = o.Guid,
                        Name = o.Name,
                        Type = o.Type
                    };

                    await _queue.Queue.Writer.WriteAsync(model);
                }

                return Accepted(new { message = "Objects queued successfully", count = obj.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error while queueing objects",
                    error = ex.Message
                });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddObject([FromBody] DTOs.AddObjectRequest obj)
        {
            if (obj == null)
                return BadRequest(new { message = "Invalid object" });

            try
            {
                var newObj = new Models.ModelObject
                {
                    Guid = obj.Guid,
                    Name = obj.Name,
                    Type = obj.Type
                };

                await _queue.Queue.Writer.WriteAsync(newObj);

                return Accepted(new { message = "Object queued successfully", id = obj.Guid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while adding the object",
                    error = ex.Message
                });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListObject([FromQuery] int limit = 100)
        {
            try
            {
                limit = Math.Clamp(limit, 1, 1000);

                var objects = await _db.Objects
                    .AsNoTracking()
                    .OrderBy(o => o.Guid)
                    .Take(limit)
                    .ToListAsync();

                return Ok(new
                {
                    message = "Objects retrieved successfully",
                    count = objects.Count,
                    objects
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving objects",
                    error = ex.Message
                });
            }
        }
    }
}
