// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Microsoft.Extensions.DependencyInjection;

using Spectre.Console.Cli;

namespace Ingenium.BuildCli.Infrastructure;

/// <summary>
/// Resolves command and service instances from a built service provider.
/// </summary>
public sealed class TypeResolver : ITypeResolver, IDisposable
{
	private readonly IServiceProvider _provider;

	/// <summary>
	/// Initializes a new instance of the <see cref="TypeResolver"/> class.
	/// </summary>
	/// <param name="provider">The root service provider.</param>
	public TypeResolver(IServiceProvider provider)
	{
		_provider = provider;
	}

	/// <inheritdoc />
	public object? Resolve(Type? type)
	{
		return type is null ? null : _provider.GetService(type);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_provider is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}
}
