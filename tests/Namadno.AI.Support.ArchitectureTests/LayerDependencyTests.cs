using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Namadno.AI.Support.Application;
using Namadno.AI.Support.Domain;
using Namadno.AI.Support.Infrastructure;

namespace Namadno.AI.Support.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(DomainAssemblyMarker).Assembly;
    private static readonly Assembly Application = typeof(ApplicationAssemblyMarker).Assembly;
    private static readonly Assembly Infrastructure = typeof(InfrastructureAssemblyMarker).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    [Fact]
    public void Domain_must_not_depend_on_outer_layers_or_infrastructure_packages()
    {
        Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Namadno.AI.Support.Application",
                "Namadno.AI.Support.Infrastructure",
                "Namadno.AI.Support.Api",
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Microsoft.AspNetCore",
                "OpenAI",
                "Pgvector")
            .GetResult()
            .IsSuccessful
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Application_must_not_depend_on_infrastructure_or_host()
    {
        Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Namadno.AI.Support.Infrastructure",
                "Namadno.AI.Support.Api",
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Microsoft.AspNetCore",
                "Pgvector")
            .GetResult()
            .IsSuccessful
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Infrastructure_must_not_depend_on_api()
    {
        Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("Namadno.AI.Support.Api")
            .GetResult()
            .IsSuccessful
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Domain_assembly_references_must_stay_framework_only()
    {
        var forbiddenPrefixes = new[]
        {
            "Namadno.AI.Support.Application",
            "Namadno.AI.Support.Infrastructure",
            "Namadno.AI.Support.Api",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "Microsoft.AspNetCore",
            "Pgvector"
        };

        Domain.GetReferencedAssemblies()
            .Select(name => name.Name)
            .Where(name => name is not null)
            .Should()
            .NotContain(name => forbiddenPrefixes.Any(prefix =>
                name!.Equals(prefix, StringComparison.Ordinal)
                || name.StartsWith(prefix + ".", StringComparison.Ordinal)));
    }

    [Fact]
    public void Api_is_the_composition_root()
    {
        Api.GetReferencedAssemblies()
            .Select(name => name.Name)
            .Should()
            .Contain("Namadno.AI.Support.Infrastructure");
    }
}
