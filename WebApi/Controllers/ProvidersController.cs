using Application.Providers.Commands;
using Application.Providers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProvidersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProvidersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<ProviderDto>>> GetProviders([FromQuery] GetProvidersQuery query)
        {
            var providers = await _mediator.Send(query);
            return Ok(providers);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProviderDetailDto>> GetProvider(Guid id)
        {
            var query = new GetProviderQuery { Id = id };
            var provider = await _mediator.Send(query);

            if (provider == null)
                return NotFound();

            return Ok(provider);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateProvider([FromBody] CreateProviderCommand command)
        {
            var providerId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetProvider), new { id = providerId }, providerId);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult> UpdateProvider(Guid id, [FromBody] UpdateProviderRequest request)
        {
            var command = new UpdateProviderCommand
            {
                Id = id,
                Name = request.Name,
                Email = request.Email,
                Specialty = request.Specialty
            };

            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeactivateProvider(Guid id)
        {
            var command = new DeactivateProviderCommand { Id = id };
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPost("{id:guid}/working-hours")]
        public async Task<ActionResult> AddWorkingHours(Guid id, [FromBody] AddWorkingHoursRequest request)
        {
            var command = new AddWorkingHoursCommand
            {
                ProviderId = id,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };

            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPost("{id:guid}/block-time")]
        public async Task<ActionResult> BlockTime(Guid id, [FromBody] BlockTimeRequest request)
        {
            var command = new BlockTimeCommand
            {
                ProviderId = id,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Reason = request.Reason
            };

            await _mediator.Send(command);
            return NoContent();
        }
    }
}