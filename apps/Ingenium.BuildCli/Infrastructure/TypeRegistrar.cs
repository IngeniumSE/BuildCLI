// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Microsoft.Extensions.DependencyInjection;

using Spectre.Console.Cli;

namespace Ingenium.BuildCli.Infrastructure;

/// <summary>
/// Adapts <see cref="IServiceCollection"/> to Spectre.Console.Cli's type registrar.
/// </summary>
public sealed class TypeRegistrar : ITypeRegistrar
{
	private readonly IServiceCollection _services;

	/// <summary>
	/// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
	/// </summary>
	/// <param name="services">The service collection to populate.</param>
	public TypeRegistrar(IServiceCollection services)
	{
		_services = services;
	}

	/// <inheritdoc />
	public ITypeResolver Build()
	{
		return new TypeResolver(_services.BuildServiceProvider());
	}

	/// <inheritdoc />
	public void Register(Type service, Type implementation)
	{
		_services.AddSingleton(service, implementation);
	}

	/// <inheritdoc />
	public void RegisterInstance(Type service, object implementation)
	{
		_services.AddSingleton(service, implementation);
	}

	/// <inheritdoc />
	public void RegisterLazy(Type service, Func<object> factory)
	{
		_services.AddSingleton(service, _ => factory());
	}
}
