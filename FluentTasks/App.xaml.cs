using FluentTasks.Core.Services;
using FluentTasks.Infrastructure.Google;
using FluentTasks.UI.Services;
using FluentTasks.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FluentTasks.UI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private readonly IHost _host;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            // Create the host for dependency injection and other services.
            _host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Infrastructure services
                    services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
                    services.AddSingleton<ITaskService, GoogleTaskService>();
                    services.AddSingleton<IconStorageService>();

                    // UI services
                    services.AddSingleton<DialogService>();
                    services.AddSingleton<IDialogService>(sp => sp.GetRequiredService<DialogService>());

                    // ViewModels
                    services.AddTransient<ShellViewModel>();
                    services.AddTransient<TaskListViewModel>();
                })
                .Build();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

        /// <summary>
        /// Gets a registered service from the DI container.
        /// </summary>
        public static T GetService<T>() where T : class
        {
            var app = Current as App;
            return app!._host.Services.GetService<T>()!;
        }
    }
}
