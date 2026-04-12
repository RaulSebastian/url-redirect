using Microsoft.Extensions.Hosting;
using UrlRedirect.Infrastructure;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddRedirectInfrastructure(context.Configuration, context.HostingEnvironment);
    })
    .Build();

host.Run();
