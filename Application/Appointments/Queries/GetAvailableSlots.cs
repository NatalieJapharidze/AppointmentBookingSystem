using Application.Common.Interfaces;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Appointments.Queries
{
    public record GetAvailableSlotsQuery : IRequest<List<TimeSlot>>
    {
        public Guid ProviderId { get; init; }
        public DateTime Date { get; init; }
        public int DurationMinutes { get; init; }
    }

    public record AppointmentTimeDto
    {
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
    }

    public class GetAvailableSlotsValidator : AbstractValidator<GetAvailableSlotsQuery>
    {
        private readonly IAppDbContext _context;

        public GetAvailableSlotsValidator(IAppDbContext context)
        {
            _context = context;

            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .MustAsync(ProviderExists)
                .WithMessage("Provider not found");

            RuleFor(x => x.Date)
                .Must(d => d.Date >= DateTime.UtcNow.Date)
                .WithMessage("Date must be today or in the future");

            RuleFor(x => x.DurationMinutes)
                .Must(d => new[] { 15, 30, 45, 60 }.Contains(d))
                .WithMessage("Duration must be 15, 30, 45, or 60 minutes");
        }

        private async Task<bool> ProviderExists(Guid providerId, CancellationToken cancellationToken)
        {
            return await _context.ServiceProviders
                .AnyAsync(p => p.Id == providerId && p.IsActive, cancellationToken);
        }
    }

    public class GetAvailableSlotsHandler : IRequestHandler<GetAvailableSlotsQuery, List<TimeSlot>>
    {
        private readonly IAppDbContext _context;

        public GetAvailableSlotsHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TimeSlot>> Handle(GetAvailableSlotsQuery request, CancellationToken cancellationToken)
        {
            var workingHours = await _context.WorkingHours
                .FirstOrDefaultAsync(wh => wh.ProviderId == request.ProviderId &&
                                         wh.DayOfWeek == request.Date.DayOfWeek &&
                                         wh.IsActive, cancellationToken);

            if (workingHours == null)
                return new List<TimeSlot>();

            var existingAppointments = await _context.Appointments
                .Where(a => a.ProviderId == request.ProviderId &&
                           a.AppointmentDate == request.Date.Date &&
                           a.Status != Domain.Enums.AppointmentStatus.Cancelled)
                .Select(a => new AppointmentTimeDto { StartTime = a.StartTime, EndTime = a.EndTime })
                .ToListAsync(cancellationToken);

            var blockedTimes = await _context.BlockedTimes
                .Where(bt => bt.ProviderId == request.ProviderId &&
                            request.Date.Date >= bt.StartDateTime.Date &&
                            request.Date.Date <= bt.EndDateTime.Date)
                .ToListAsync(cancellationToken);

            return GenerateAvailableSlots(
                workingHours.StartTime,
                workingHours.EndTime,
                request.DurationMinutes,
                existingAppointments,
                blockedTimes,
                request.Date);
        }

        private static List<TimeSlot> GenerateAvailableSlots(
            TimeOnly workingStart,
            TimeOnly workingEnd,
            int durationMinutes,
            List<AppointmentTimeDto> existingAppointments,
            List<Domain.Entities.BlockedTime> blockedTimes,
            DateTime date)
        {
            var availableSlots = new List<TimeSlot>();
            var currentTime = workingStart;

            while (currentTime.AddMinutes(durationMinutes) <= workingEnd)
            {
                var proposedEndTime = currentTime.AddMinutes(durationMinutes);

                var hasAppointmentConflict = existingAppointments.Any(a =>
                    a.StartTime < proposedEndTime && a.EndTime > currentTime);

                if (!hasAppointmentConflict)
                {
                    var proposedStart = date.Add(currentTime.ToTimeSpan());
                    var proposedEnd = date.Add(proposedEndTime.ToTimeSpan());

                    var hasBlockedConflict = blockedTimes.Any(bt =>
                        proposedStart < bt.EndDateTime && proposedEnd > bt.StartDateTime);

                    if (!hasBlockedConflict)
                    {
                        availableSlots.Add(new TimeSlot(currentTime, proposedEndTime));
                    }
                }

                currentTime = currentTime.AddMinutes(15);
            }

            return availableSlots;
        }
    }
}