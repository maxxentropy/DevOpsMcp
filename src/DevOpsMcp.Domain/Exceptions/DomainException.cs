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

public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityType, string id) 
        : base($"{entityType}.NotFound", $"{entityType} with id '{id}' was not found")
    {
    }
}

public sealed class InvalidOperationException : DomainException
{
    public InvalidOperationException(string operation, string reason) 
        : base($"Operation.{operation}.Invalid", $"Cannot perform {operation}: {reason}")
    {
    }
}

public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string rule, string message) 
        : base($"BusinessRule.{rule}", message)
    {
    }
}