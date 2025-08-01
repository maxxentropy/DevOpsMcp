namespace DevOpsMcp.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string Code { get; }
    
    protected DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
    
    protected DomainException(string code, string message, Exception innerException) 
        : base(message, innerException)
    {
        Code = code;
    }
}

public sealed class EntityNotFoundException(string entityType, string id)
    : DomainException($"{entityType}.NotFound", $"{entityType} with id '{id}' was not found");

public sealed class InvalidOperationException(string operation, string reason)
    : DomainException($"Operation.{operation}.Invalid", $"Cannot perform {operation}: {reason}");

public sealed class BusinessRuleViolationException(string rule, string message)
    : DomainException($"BusinessRule.{rule}", message);