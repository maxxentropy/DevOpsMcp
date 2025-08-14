using System.Diagnostics.CodeAnalysis;

// Test assemblies should not be analyzed for CA1515
[assembly: SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Test classes must be public for xUnit discovery")]