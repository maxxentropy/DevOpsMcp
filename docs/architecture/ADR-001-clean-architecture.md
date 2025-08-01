# ADR-001: Clean Architecture Implementation

## Status
Accepted

## Context
We need to build a maintainable, testable, and scalable MCP server for Azure DevOps integration. The system should be flexible enough to support multiple protocols (SSE, stdio, HTTP) and authentication methods while maintaining clear separation of concerns.

## Decision
We will implement Clean Architecture (also known as Onion Architecture) with the following layers:

1. **Domain Layer** (`DevOpsMcp.Domain`)
   - Contains business entities, value objects, domain events, and interfaces
   - Has no dependencies on other layers
   - Represents the core business logic

2. **Application Layer** (`DevOpsMcp.Application`)
   - Contains use cases, command/query handlers (CQRS), and application services
   - Depends only on Domain layer
   - Orchestrates the flow of data and business rules

3. **Infrastructure Layer** (`DevOpsMcp.Infrastructure`)
   - Contains implementations of domain interfaces
   - Handles external concerns (Azure DevOps API, caching, persistence)
   - Depends on Domain and Application layers

4. **Server Layer** (`DevOpsMcp.Server`)
   - Contains MCP protocol implementations and API endpoints
   - Handles presentation concerns
   - Depends on all other layers

5. **Contracts Layer** (`DevOpsMcp.Contracts`)
   - Contains DTOs and external contracts
   - Can be referenced by any layer
   - Ensures consistent API contracts

## Consequences

### Positive
- Clear separation of concerns
- Highly testable (each layer can be tested independently)
- Flexible and maintainable
- Domain logic is isolated from infrastructure concerns
- Easy to swap implementations (e.g., different auth providers)
- Supports Domain-Driven Design principles

### Negative
- More initial complexity
- More projects to manage
- Potential for over-engineering simple features
- Learning curve for developers unfamiliar with the pattern

## References
- Clean Architecture by Robert C. Martin
- Domain-Driven Design by Eric Evans
- Implementing Domain-Driven Design by Vaughn Vernon