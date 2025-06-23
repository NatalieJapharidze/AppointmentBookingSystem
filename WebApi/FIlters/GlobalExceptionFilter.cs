using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Domain.Common;

namespace WebApi.FIlters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            var response = exception switch
            {
                BusinessRuleException businessRuleException => new BadRequestObjectResult(new
                {
                    title = "Business rule violation",
                    message = businessRuleException.Message
                }),
                ValidationException validationException => new BadRequestObjectResult(new
                {
                    title = "Validation failed",
                    errors = validationException.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
                }),
                ArgumentException argumentException => new BadRequestObjectResult(new
                {
                    title = "Invalid argument",
                    message = argumentException.Message
                }),
                InvalidOperationException invalidOperationException => new BadRequestObjectResult(new
                {
                    title = "Invalid operation",
                    message = invalidOperationException.Message
                }),
                _ => new ObjectResult(new
                {
                    title = "An error occurred",
                    message = "An unexpected error occurred"
                })
                {
                    StatusCode = 500
                }
            };

            if (exception is BusinessRuleException || exception is ValidationException)
            {
                _logger.LogWarning("Business/Validation error: {Message}", exception.Message);
            }
            else
            {
                _logger.LogError(exception, "Unhandled exception occurred");
            }

            context.Result = response;
            context.ExceptionHandled = true;
        }
    }
}