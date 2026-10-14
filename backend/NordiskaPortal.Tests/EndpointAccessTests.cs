using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NordiskaPortal.API.Controllers;

namespace NordiskaPortal.Tests;

public class EndpointAccessTests
{
    private static readonly string[] AnonymousOnPurpose =
    {
        "AuthController.Register",
        "AuthController.LoginPin",
        "AuthController.Refresh",
        "HealthController.GetHealth",
    };

    private static List<(string Name, bool IsAnonymous)> Actions()
    {
        var result = new List<(string, bool)>();

        var controllers = typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var controller in controllers)
        {
            var classAuthorized = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any();
            var classAnonymous = controller.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();

            var actions = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any());

            foreach (var action in actions)
            {
                var methodAuthorized = action.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any();
                var methodAnonymous = action.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();

                var anonymous = methodAnonymous || classAnonymous || !(classAuthorized || methodAuthorized);
                result.Add(($"{controller.Name}.{action.Name}", anonymous));
            }
        }

        return result;
    }

    [Fact]
    public void The_check_finds_the_controller_actions()
    {
        Actions().Count.Should().BeGreaterThan(10);
    }

    [Fact]
    public void Only_the_intended_endpoints_are_anonymous()
    {
        var anonymous = Actions().Where(a => a.IsAnonymous).Select(a => a.Name).ToList();

        anonymous.Should().BeEquivalentTo(AnonymousOnPurpose);
    }
}