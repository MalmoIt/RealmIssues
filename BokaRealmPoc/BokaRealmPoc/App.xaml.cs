using System;
using System.Diagnostics;
using Prism;
using Prism.Ioc;
using BokaRealmPoc.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Prism.DryIoc;
using Realms;
using Realms.Sync;
using Realms.Sync.Exceptions;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace BokaRealmPoc
{
    public partial class App : PrismApplication
    {
        /* 
         * The Xamarin Forms XAML Previewer in Visual Studio uses System.Activator.CreateInstance.
         * This imposes a limitation in which the App class must have a default constructor. 
         * App(IPlatformInitializer initializer = null) cannot be handled by the Activator.
         */
        public App() : this(null) { }

        public App(IPlatformInitializer initializer) : base(initializer) { }

        protected override async void OnInitialized()
        {
            InitializeComponent();
            
            Session.Error += (sender, errorArgs) =>
            {
                var session = (Session) sender;
                var sessionException = (SessionException)errorArgs.Exception;
                if (sessionException.ErrorCode.IsClientResetError())
                {
                    var clientResetException = (ClientResetException)errorArgs.Exception;
                    //CloseRealmSafely();
                    //SaveBackupRealmPath(clientResetException.BackupFilePath);
                    clientResetException.InitiateClientReset();
                }
                else
                {
                    Debug.WriteLine($"SessionError ErrorCode | {sessionException?.ErrorCode}");
                    Debug.WriteLine($"SessionError Message | {sessionException?.Message}");
                    Debug.WriteLine($"SessionError InnerMessage | {sessionException?.InnerException?.Message}");
                }

                


            };

            await NavigationService.NavigateAsync($"{nameof(NavigationPage)}/{nameof(LoginPage)}");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<LoginPage>();
            containerRegistry.RegisterForNavigation<MainPage>();
            containerRegistry.RegisterForNavigation<EditNotePage>();
            containerRegistry.RegisterForNavigation<AddLandPage>();
        }
    }
}
