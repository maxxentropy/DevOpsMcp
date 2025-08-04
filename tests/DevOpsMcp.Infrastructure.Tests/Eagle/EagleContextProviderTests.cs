using System;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Infrastructure.Eagle;
using Eagle;
using Eagle._Components.Public;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace DevOpsMcp.Infrastructure.Tests.Eagle;

public sealed class EagleContextProviderTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly EagleContextProvider _contextProvider;
    private readonly ILogger<EagleContextProvider> _logger;

    public EagleContextProviderTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<EagleContextProvider>>();
        var mockSessionStore = new Mock<IEagleSessionStore>();
        var mockMcpCallTool = new Mock<IMcpCallToolCommand>();
        _contextProvider = new EagleContextProvider(_serviceProvider, _logger, mockSessionStore.Object, mockMcpCallTool.Object);
    }

    [Fact]
    public void InjectRichContext_Should_Create_Context_Commands()
    {
        // Arrange
        using var interpreter = CreateTestInterpreter();
        var devOpsContext = CreateTestContext();

        // Act
        var interpreterAdapter = new EagleInterpreterAdapter(interpreter);
        _contextProvider.InjectRichContext(interpreterAdapter, devOpsContext);

        // Assert - Check if commands were created
        var result = EvaluateScript(interpreter, "info commands mcp::*");
        result.Should().Contain("mcp::context");
        result.Should().Contain("mcp::session");
        result.Should().Contain("mcp::call_tool");
    }

    [Fact]
    public void Context_Command_Should_Return_User_Data()
    {
        // Arrange
        using var interpreter = CreateTestInterpreter();
        var devOpsContext = CreateTestContext();
        var interpreterAdapter = new EagleInterpreterAdapter(interpreter);
        _contextProvider.InjectRichContext(interpreterAdapter, devOpsContext);

        // Act & Assert
        var userName = EvaluateScript(interpreter, "mcp::context get user.name");
        userName.Should().Be("DevOps User");

        var userRole = EvaluateScript(interpreter, "mcp::context get user.role");
        userRole.Should().Be("Developer");
    }

    [Fact]
    public void Context_Command_Should_Return_Project_Data()
    {
        // Arrange
        using var interpreter = CreateTestInterpreter();
        var devOpsContext = CreateTestContext();
        var interpreterAdapter = new EagleInterpreterAdapter(interpreter);
        _contextProvider.InjectRichContext(interpreterAdapter, devOpsContext);

        // Act & Assert
        var projectName = EvaluateScript(interpreter, "mcp::context get project.name");
        projectName.Should().Be("DevOps MCP Project");

        var projectStage = EvaluateScript(interpreter, "mcp::context get project.stage");
        projectStage.Should().Be("Development");
    }

    [Fact]
    public void Session_Command_Should_Store_And_Retrieve_Values()
    {
        // Arrange
        using var interpreter = CreateTestInterpreter();
        var interpreterAdapter = new EagleInterpreterAdapter(interpreter);
        _contextProvider.InjectRichContext(interpreterAdapter, null);

        // Act
        EvaluateScript(interpreter, "mcp::session set testKey testValue");
        var retrievedValue = EvaluateScript(interpreter, "mcp::session get testKey");

        // Assert
        retrievedValue.Should().Be("testValue");
    }

    [Fact]
    public void Session_Command_Should_List_Keys()
    {
        // Arrange
        using var interpreter = CreateTestInterpreter();
        var interpreterAdapter = new EagleInterpreterAdapter(interpreter);
        _contextProvider.InjectRichContext(interpreterAdapter, null);

        // Act
        EvaluateScript(interpreter, "mcp::session set key1 value1");
        EvaluateScript(interpreter, "mcp::session set key2 value2");
        var keys = EvaluateScript(interpreter, "mcp::session list");

        // Assert
        keys.Should().Contain("key1");
        keys.Should().Contain("key2");
    }

    [Fact]
    public void Session_Command_Should_Clear_All_Values()
    {
        // Arrange
        using var interpreter = CreateTestInterpreter();
        var interpreterAdapter = new EagleInterpreterAdapter(interpreter);
        _contextProvider.InjectRichContext(interpreterAdapter, null);

        // Act
        EvaluateScript(interpreter, "mcp::session set key1 value1");
        EvaluateScript(interpreter, "mcp::session clear");
        var keys = EvaluateScript(interpreter, "mcp::session list");

        // Assert
        keys.Should().BeEmpty();
    }

    [Fact]
    public void CallTool_Command_Should_Return_Placeholder_Response()
    {
        // Arrange
        using var interpreter = CreateTestInterpreter();
        var interpreterAdapter = new EagleInterpreterAdapter(interpreter);
        _contextProvider.InjectRichContext(interpreterAdapter, null);

        // Act
        var result = EvaluateScript(interpreter, "mcp::call_tool test_tool arg1 arg2");

        // Assert
        result.Should().Contain("Tool 'test_tool' called");
        result.Should().Contain("arg1 arg2");
    }

    [Fact]
    public void Context_Command_Should_Handle_Invalid_Action()
    {
        // Arrange
        using var interpreter = CreateTestInterpreter();
        var interpreterAdapter = new EagleInterpreterAdapter(interpreter);
        _contextProvider.InjectRichContext(interpreterAdapter, null);

        // Act & Assert
        Action act = () => EvaluateScript(interpreter, "mcp::context invalid_action");
        act.Should().Throw<Exception>().WithMessage("*Unknown context action*");
    }

    private Interpreter CreateTestInterpreter()
    {
        Result? result = null;
        var interpreter = Interpreter.Create(ref result);
        
        #pragma warning disable CA1508 // Defensive null check for external API
        return interpreter ?? throw new InvalidOperationException($"Failed to create interpreter: {result}");
        #pragma warning restore CA1508
    }

    private string EvaluateScript(Interpreter interpreter, string script)
    {
        Result? result = null;
        var returnCode = interpreter.EvaluateScript(script, ref result);
        
        if (returnCode != ReturnCode.Ok)
            throw new Exception($"Script evaluation failed: {result}");
            
        return result?.ToString() ?? string.Empty;
    }

    private DevOpsContext CreateTestContext()
    {
        return new DevOpsContext
        {
            User = new UserProfile
            {
                Name = "DevOps User",
                Role = "Developer",
                ExperienceLevel = "Senior"
            },
            Project = new ProjectMetadata
            {
                Name = "DevOps MCP Project",
                Stage = "Development",
                Type = "Microservices"
            },
            Environment = new EnvironmentContext
            {
                EnvironmentType = "Development",
                IsProduction = false
            },
            TechStack = new TechnologyConfiguration
            {
                CloudProvider = "Azure",
                CiCdPlatform = "Azure DevOps"
            }
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _serviceProvider?.Dispose();
        }
    }
}