using Application.Appointments.Commands;
using Application.Appointments.Queries;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AppointmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<CreateAppointmentResult>> CreateAppointment([FromBody] CreateAppointmentCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetAppointment), new { id = result.AppointmentId }, result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AppointmentDetailDto>> GetAppointment(Guid id)
        {
            var query = new GetAppointmentQuery { Id = id };
            var appointment = await _mediator.Send(query);

            if (appointment == null)
                return NotFound();

            return Ok(appointment);
        }

        [HttpGet]
        public async Task<ActionResult<List<AppointmentDto>>> GetAppointments([FromQuery] GetAppointmentsQuery query)
        {
            var appointments = await _mediator.Send(query);
            return Ok(appointments);
        }

        [HttpGet("available-slots")]
        public async Task<ActionResult<List<TimeSlot>>> GetAvailableSlots([FromQuery] GetAvailableSlotsQuery query)
        {
            var slots = await _mediator.Send(query);
            return Ok(slots);
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<ActionResult> CancelAppointment(Guid id, [FromBody] CancelAppointmentRequest request)
        {
            var command = new CancelAppointmentCommand { AppointmentId = id, Reason = request.Reason };
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPut("{id:guid}/reschedule")]
        public async Task<ActionResult> RescheduleAppointment(Guid id, [FromBody] RescheduleAppointmentRequest request)
        {
            var command = new RescheduleAppointmentCommand
            {
                AppointmentId = id,
                NewDate = request.NewAppointmentDate,
                NewStartTime = request.NewStartTime,
                DurationMinutes = request.DurationMinutes
            };
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPut("{id:guid}/complete")]
        public async Task<ActionResult> CompleteAppointment(Guid id)
        {
            var command = new CompleteAppointmentCommand { AppointmentId = id };
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPut("{id:guid}/no-show")]
        public async Task<ActionResult> MarkAsNoShow(Guid id)
        {
            var command = new MarkAsNoShowCommand { AppointmentId = id };
            await _mediator.Send(command);
            return NoContent();
        }
    }
}