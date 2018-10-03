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
        private string _username = "andersMonday";

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public DelegateCommand LoginCommand { get; set; }
        public DelegateCommand WipeCommand { get; set; }

        public LoginPageViewModel(INavigationService navigationService) : base(navigationService)
        {
            LoginCommand = new DelegateCommand(Login);
            WipeCommand = new DelegateCommand(Wipe);
        }

        private async void Wipe()
        {
            if (string.IsNullOrEmpty(Username)) return;
            await ResetUser(Username);

            var user = User.Current;
            RealmConfiguration.DefaultConfiguration = new QueryBasedSyncConfiguration(user: user);
            Realm.DeleteRealm(RealmConfiguration.DefaultConfiguration);
        }

        private async void Login()
        {
            if (string.IsNullOrEmpty(Username)) return;
            await ResetUser(Username);
            
            await NavigationService.NavigateAsync($"{nameof(MainPage)}");
        }

        private async Task ResetUser(string username)
        {
            try
            {
                var users = User.AllLoggedIn;

                foreach (var user1 in users)
                {
                    await user1.LogOutAsync();
                }
                if (User.Current != null) await User.Current.LogOutAsync();


                //var credentials = Credentials.Nickname("andersMonday");
                var credentials = Credentials.Nickname(username);
                //var credentials = Credentials.Nickname("andersMondayAcl");
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
