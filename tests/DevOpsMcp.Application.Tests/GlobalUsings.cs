global using Xunit;
global using FluentAssertions;
global using Moq;
global using MediatR;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Caching.Memory;
global using DevOpsMcp.Application.Commands.Projects;
global using DevOpsMcp.Application.Commands.WorkItems;
global using DevOpsMcp.Application.Queries.Projects;
global using DevOpsMcp.Application.Queries.WorkItems;
global using DevOpsMcp.Domain.Entities;
global using DevOpsMcp.Domain.Interfaces;
global using DevOpsMcp.Domain.ValueObjects;
global using DevOpsMcp.Contracts.Projects;
global using DevOpsMcp.Contracts.WorkItems;