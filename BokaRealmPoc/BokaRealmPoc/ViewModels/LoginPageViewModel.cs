using System;
using System.Threading.Tasks;
using BokaRealmPoc.Views;
using Prism.Commands;
using Prism.Navigation;
using Realms;
using Realms.Sync;

namespace BokaRealmPoc.ViewModels
{
    public class LoginPageViewModel : ViewModelBase
    {
        public DelegateCommand LoginCommand { get; set; }
        public DelegateCommand WipeCommand { get; set; }

        public LoginPageViewModel(INavigationService navigationService) : base(navigationService)
        {
            LoginCommand = new DelegateCommand(Login);
            WipeCommand = new DelegateCommand(Wipe);
        }

        private async void Wipe()
        {
            await ResetUser();

            var user = User.Current;
            var config = new FullSyncConfiguration(new Uri("/~/nordmannTwo", UriKind.Relative), user);
            Realm.DeleteRealm(config);
        }

        private async void Login()
        {
            await ResetUser();
            
            await NavigationService.NavigateAsync($"{nameof(MainPage)}");
        }

        private async Task ResetUser()
        {
            try
            {
                var users = User.AllLoggedIn;

                foreach (var user1 in users)
                {
                    await user1.LogOutAsync();
                }
                if (User.Current != null) await User.Current.LogOutAsync();

                var credentials = Credentials.Nickname("andersMonday");
                var user = await User.LoginAsync(credentials, new Uri("https://bokapoc.de1a.cloud.realm.io"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
