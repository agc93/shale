using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shale;

public interface IPlugin {
	IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration? config);
}