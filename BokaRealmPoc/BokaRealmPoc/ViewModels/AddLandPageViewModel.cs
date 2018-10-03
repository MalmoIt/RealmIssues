using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using BokaRealmPoc.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Realms;
using Realms.Sync;

namespace BokaRealmPoc.ViewModels
{
	public class AddLandPageViewModel : ViewModelBase
	{
	    private List<LandNoteViewModel> _lands;
	    private IDisposable _token;
	    private Note _note;

	    public DelegateCommand AddLandCommand { get; set; }
	    public DelegateCommand AddManyLandsCommand { get; set; }

	    public List<LandNoteViewModel> Lands
	    {
	        get => _lands;
	        set => SetProperty(ref _lands, value);
	    }

	    public AddLandPageViewModel(INavigationService navigationService) : base(navigationService)
	    {
	        AddLandCommand = new DelegateCommand(OnAdd);
	        AddManyLandsCommand = new DelegateCommand(OnAddMany);
	    }

	    private void OnAddMany()
	    {
	        var realm = Realm.GetInstance();
	     
            realm.Write(() =>
            {
                var user = PermissionUser.Get(realm, User.Current.Identity);
                var permission = Permission.Get<Note>(user.Role, realm);

                permission.CanCreate = true;
                permission.CanDelete = true;
                permission.CanRead = true;
                permission.CanSetPermissions = true;
                permission.CanUpdate = true;
                permission.CanQuery = true;

                for (int i = 0; i < 100; i++)
                {
                    var land = new Land
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = $"Land {Guid.NewGuid().ToString().Substring(1,3)}"
                    };

                    land.Permissions.Add(permission);

                realm.Add(land);
                }
            });
            
	    }

	    private void OnAdd()
	    {
	        var realm = Realm.GetInstance();
            
            realm.Write(() =>
            {
                var user = PermissionUser.Get(realm, User.Current.Identity);
                var permission = Permission.Get<Note>(user.Role, realm);

                permission.CanCreate = true;
                permission.CanDelete = true;
                permission.CanRead = true;
                permission.CanSetPermissions = true;
                permission.CanUpdate = true;
                permission.CanQuery = true;

                var land = new Land
	            {
	                Id = Guid.NewGuid().ToString(),
	                Title = $"Land {Guid.NewGuid().ToString().Substring(1, 3)}"
	            };

                land.Permissions.Add(permission);

	            realm.Add(land);
	        });
	    }

	    public override void OnNavigatedTo(NavigationParameters parameters)
	    {
	        var realm = Realm.GetInstance();

            var noteId = (string) parameters["noteId"];
	        _note = realm.Find<Note>(noteId);

	        _note.PropertyChanged += (sender, args) =>
	        {
	            Debug.WriteLine($"Note changed {args.PropertyName}");
	        };
  
            _token = realm.All<LandNote>().SubscribeForNotifications((sender, changes, error) =>
            {
                Debug.WriteLine($"SubscribeForNotifications NewModifiedIndices: {changes?.NewModifiedIndices?.Length}");
                Debug.WriteLine($"SubscribeForNotifications DeletedIndices: {changes?.DeletedIndices?.Length}");
                Debug.WriteLine($"SubscribeForNotifications InsertedIndices: {changes?.InsertedIndices?.Length}");
                Debug.WriteLine($"SubscribeForNotifications ModifiedIndices: {changes?.ModifiedIndices?.Length}");
                Debug.WriteLine($"SubscribeForNotifications Moves: {changes?.Moves?.Length}");

                Lands = new List<LandNoteViewModel>(realm.All<LandNote>().ToList().Select(x => new LandNoteViewModel(x.Id, x.Land.Title)));

                UpdateSelectedLands();
            });

            //Notes = new ObservableCollection<NoteViewModel>(_realm.All<Note>().ToList().Select(x => new NoteViewModel(x)));
	        Lands = new List<LandNoteViewModel>(realm.All<LandNote>().ToList().Select(x => new LandNoteViewModel(x.Id, x.Land.Title)));
	        UpdateSelectedLands();
            var subscription = realm.All<LandNote>().Subscribe();
            
            subscription.PropertyChanged += (sender, args) =>
            {
                switch (subscription.State)
                {
                    case SubscriptionState.Creating:
                        Debug.WriteLine($"SubscriptionState.Creating | {subscription.Results?.Count()}");
                        // The subscription has not yet been written to the Realm
                        break;
                    case SubscriptionState.Pending:
                        Debug.WriteLine($"SubscriptionState.Pending | {subscription.Results?.Count()}");
                        // The subscription has been written to the Realm and is waiting
                        // to be processed by the server
                        break;
                    case SubscriptionState.Complete:

                        Lands = new List<LandNoteViewModel>(realm.All<LandNote>().ToList().Select(x => new LandNoteViewModel(x.Id, x.Land.Title)));
                        UpdateSelectedLands();
                        Debug.WriteLine($"SubscriptionState.Complete | {subscription.Results?.Count()}");
                        // The subscription has been processed by the server and all objects
                        // matching the query are in the local Realm
                        break;
                    case SubscriptionState.Invalidated:
                        Debug.WriteLine($"SubscriptionState.Invalidated | {subscription.Results?.Count()}");
                        // The subscription has been removed
                        break;
                    case SubscriptionState.Error:
                        Debug.WriteLine($"SubscriptionState.Error | {subscription.Results?.Count()}");
                        // An error occurred while processing the subscription
                        var error = subscription.Error;
                        break;
                }
            };
        }

	    private void UpdateSelectedLands()
	    {
	        foreach (var noteLand in _note.Lands)
	        {
	            foreach (var landViewModel in Lands)
	            {
	                if (noteLand.Id == landViewModel.Id)
	                {
	                    landViewModel.IsSelected = true;
	                }
	            }
	        }
        }

	    public override void Destroy()
	    {
            _token?.Dispose();
	    }

	    public void SelectedLand(LandNoteViewModel landNoteViewModel)
	    {
	        var realm = Realm.GetInstance();

	        var land = realm.Find<LandNote>(landNoteViewModel.Id);

	        if (landNoteViewModel.IsSelected)
	        {
                // Remove
	            realm.Write(() =>
	            {
	                if (_note.Lands.Contains(land))
	                {
	                    _note.Lands.Remove(land);
	                }
	            });
            }
	        else
	        { 
                // Add
	            realm.Write(() =>
	            {

	                _note.Lands.Add(land);
	            });
            }

	        landNoteViewModel.IsSelected = !landNoteViewModel.IsSelected;
	    }
	}

    public class LandNoteViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Id { get; }
        public string Title { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public LandNoteViewModel(string id, string title)
        {
            Id = id;
            Title = title;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

