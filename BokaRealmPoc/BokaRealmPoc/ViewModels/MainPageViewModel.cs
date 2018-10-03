using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BokaRealmPoc.Models;
using BokaRealmPoc.Views;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using Realms;
using Realms.Sync;
using Xamarin.Forms;

namespace BokaRealmPoc.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private readonly IPageDialogService _pageDialogService;

        public int Counter
        {
            get => _counter;
            set => SetProperty(ref _counter, value);
        }

        public bool ShowButton
        {
            get => _showButton;
            set => SetProperty(ref _showButton, value);
        }

        public bool ShowList
        {
            get => _showList;
            set => SetProperty(ref _showList, value);
        }

        private bool _isLoading;
        //private IEnumerable<Note> _notes;
        private ObservableCollection<ListGroup> _notes = new ObservableCollection<ListGroup>();
        //private ObservableCollection<NoteViewModel> _notes = new ObservableCollection<NoteViewModel>();
        //private QueryBasedSyncConfiguration _configuration;
        private IDisposable _token; // TODO Dispose...
        private int _counter;
        private Note _selectedItem;
        private Realm _realm;
        private IDisposable _sessionToken;
        private bool _showButton = true;
        private bool _showList;
        private IDisposable _sessionUploadToken;
        public DelegateCommand AddNoteCommand { get; set; }
        public DelegateCommand AddPermissionNoteCommand { get; set; }
        public DelegateCommand AddManyCommand { get; set; }
        public DelegateCommand DeleteAllCommand { get; set; }
        public DelegateCommand GetNotesCommand { get; set; }

        public NoteViewModel SelectedItem
        {
            get => null;
            set
            {
                if (value == null) return;
                NavigateToNote(value);
            }
        }

        private async void NavigateToNote(NoteViewModel noteViewModel)
        {
            await NavigationService.NavigateAsync($"{nameof(EditNotePage)}?noteId={noteViewModel.Note.Id}");
        }

        //public IEnumerable<Note> Notes
        //{
        //    get => _notes;
        //    set => SetProperty(ref _notes, value);
        //}

        public ObservableCollection<ListGroup> Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public MainPageViewModel(
            INavigationService navigationService,
            IPageDialogService pageDialogService)
            : base(navigationService)
        {
            _pageDialogService = pageDialogService;
            AddNoteCommand = new DelegateCommand(AddNote);
            AddPermissionNoteCommand = new DelegateCommand(AddWithPermissionOtherUser);
            AddManyCommand = new DelegateCommand(AddMany);
            DeleteAllCommand = new DelegateCommand(DeleteAll);
            GetNotesCommand = new DelegateCommand(GetNotes);
        }

        private void GetNotes()
        {
            ShowButton = false;
            ShowList = true;
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var user = User.Current;
            ////_configuration = new FullSyncConfiguration(new Uri("/~/nordmannTwo", UriKind.Relative), user);
            //////_configuration = new QueryBasedSyncConfiguration(new Uri("/boka-realm", UriKind.Relative));

            //var serverURL = new System.Uri("realms://bokapoc.de1a.cloud.realm.io");

            RealmConfiguration.DefaultConfiguration = new QueryBasedSyncConfiguration(user: user);

            _realm = Realm.GetInstance();
            _realm.Error += (sender, args) =>
            {
                var k = args;
                var l = sender;
            };

            var session = _realm.GetSession();

            //var token = session.GetProgressObservable(ProgressDirection.Download, ProgressMode.ReportIndefinitely)
            //    .Subscribe(progress =>
            //    {
            //        if (progress.TransferredBytes < progress.TransferableBytes)
            //        {
            //            // Show progress indicator
            //        }
            //        else
            //        {
            //            // Hide the progress indicator
            //        }
            //    });

            _sessionToken = session.GetProgressObservable(ProgressDirection.Download, ProgressMode.ReportIndefinitely)
                .Subscribe(new PocSync());
            _sessionUploadToken = session.GetProgressObservable(ProgressDirection.Upload, ProgressMode.ReportIndefinitely)
                .Subscribe(new PocSync());

            var internalStopWatch = new Stopwatch();
            internalStopWatch.Start();

            _token = _realm.All<Note>().SubscribeForNotifications((sender, changes, error) =>
            {
                Debug.WriteLine($"SubscribeForNotifications NewModifiedIndices: {changes?.NewModifiedIndices?.Length}");
                Debug.WriteLine($"SubscribeForNotifications DeletedIndices: {changes?.DeletedIndices?.Length}");
                Debug.WriteLine($"SubscribeForNotifications InsertedIndices: {changes?.InsertedIndices?.Length}");
                Debug.WriteLine($"SubscribeForNotifications ModifiedIndices: {changes?.ModifiedIndices?.Length}");
                Debug.WriteLine($"SubscribeForNotifications Moves: {changes?.Moves?.Length}");

                Notes = new ObservableCollection<ListGroup>(CreateGroupedNotes(_realm.All<Note>().ToList()));
                Counter = Notes.Count;
                internalStopWatch.Stop();

                if (Notes.Count > 0)
                {
                    //Device.BeginInvokeOnMainThread(async () =>
                    //{
                    //    await _pageDialogService.DisplayAlertAsync("Notater hentet",
                    //        $"SubscribeForNotifications:{internalStopWatch.Elapsed.TotalSeconds} sekunder",
                    //        "OK");
                    //});
                }
                
                Debug.WriteLine($"SubscribeForNotifications: STOPWATCH ELAPSED {internalStopWatch.Elapsed.TotalSeconds}");
            });

            //Notes = new ObservableCollection<NoteViewModel>(_realm.All<Note>().ToList().Select(x => new NoteViewModel(x)));
            Notes = new ObservableCollection<ListGroup>(CreateGroupedNotes(_realm.All<Note>().ToList()));

            var subscription = _realm.All<Note>().Subscribe();

            var subscriptionStopwatch = new Stopwatch();
            subscriptionStopwatch.Start();

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
                        subscriptionStopwatch.Stop();

                        Notes = new ObservableCollection<ListGroup>(CreateGroupedNotes(subscription.Results.ToList()));

                        Counter = Notes.Count();

                        if (Notes.Count > 0)
                        {
                            //Device.BeginInvokeOnMainThread(async () =>
                            //{
                            //    await _pageDialogService.DisplayAlertAsync("Notater hentet",
                            //        $"PropertyChanged:{internalStopWatch.Elapsed.TotalSeconds} sekunder",
                            //        "OK");
                            //});
                        }

                        Debug.WriteLine($"SubscriptionState.Complete | {subscription.Results?.Count()}");
                        // The subscription has been processed by the server and all objects
                        // matching the query are in the local Realm
                        break;
                    case SubscriptionState.Invalidated:
                        subscriptionStopwatch.Stop();
                        Debug.WriteLine($"SubscriptionState.Invalidated | {subscription.Results?.Count()}");
                        // The subscription has been removed
                        break;
                    case SubscriptionState.Error:
                        subscriptionStopwatch.Stop();
                        Debug.WriteLine($"SubscriptionState.Error | {subscription.Results?.Count()}");
                        // An error occurred while processing the subscription
                        var error = subscription.Error;
                        break;
                }

                Debug.WriteLine(
                    $"subscriptionStopwatch: STOPWATCH ELAPSED {subscriptionStopwatch.Elapsed.TotalSeconds}");
            };

            stopwatch.Stop();

            if (Notes.Count > 0)
            {
                //Device.BeginInvokeOnMainThread(async () =>
                //{
                //    await _pageDialogService.DisplayAlertAsync("Notater hentet",
                //        $"GetNotes:{internalStopWatch.Elapsed.TotalSeconds} sekunder",
                //        "OK");
                //});
            }

            Debug.WriteLine($"GetNotes END OF METHOD: STOPWATCH ELAPSED {stopwatch.Elapsed.TotalSeconds}");
        }

        public override void Destroy()
        {
            _token?.Dispose();
        }

        private async void DeleteAll()
        {
            IsLoading = true;
            var realm = await Realm.GetInstanceAsync();
            realm.Write(() => { realm.RemoveAll<Note>(); });
            IsLoading = false;
        }

        async void CloseRealmSafely()
        {
            var realm = await Realm.GetInstanceAsync();
            realm.Dispose();

            Realm.DeleteRealm(RealmConfiguration.DefaultConfiguration);
            // Safely dispose the realm instances, possibly notify the user
        }

        void SaveBackupRealmPath(string path)
        {
            // Persist the location of the backup realm
        }

        public override async void OnNavigatedTo(NavigationParameters parameters)
        {

        }
        private void OnError(object sender, ErrorEventArgs e)
        {
            var k = e;

        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private void AddMany()
        {
            var random = new Random();
            _realm.Write(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    var year = random.Next(2010, 2019);
                    var month = random.Next(1, 13);
                    var day = random.Next(1, 20);
                    
                    var note = new Note
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = $"Test {i}",
                        DueDate = new DateTimeOffset(year, month, day, 1, 1, 1, TimeSpan.Zero)
                    };

                    var landId = Guid.NewGuid().ToString();

                    var land = new Land
                    {
                        Id = landId,
                        Title = $"Land {landId.Substring(0, 4)}"
                    };

                    var landNote = new LandNote
                    {
                        Id = $"{note.Id}+{land.Id}",
                        Land = land,
                        Note = note
                    };

                    var user = PermissionUser.Get(_realm, User.Current.Identity);

                    var permission = Permission.Get<Note>(user.Role, _realm);

                    permission.CanCreate = true;
                    permission.CanDelete = true;
                    permission.CanRead = true;
                    permission.CanSetPermissions = true;
                    permission.CanUpdate = true;
                    permission.CanQuery = true;

                    note.Permissions.Add(permission);
                    landNote.Permissions.Add(permission);
                    land.Permissions.Add(permission);

                    note.Lands.Add(landNote);

                    _realm.Add(note);
                }
            });
        }

        private void AddWithPermissionOtherUser()
        {
            _realm.Write(() =>
            {
                var guid = Guid.NewGuid().ToString();

                var note = new Note
                {
                    Id = guid,
                    Title = $"Test {guid.Substring(0, 4)}"
                };

                var user = PermissionUser.Get(_realm, User.Current.Identity);

                var permission = Permission.Get<Note>(user.Role, _realm);

                permission.CanCreate = true;
                permission.CanDelete = true;
                permission.CanRead = true;
                permission.CanSetPermissions = true;
                permission.CanUpdate = true;
                permission.CanQuery = true;

                note.Permissions.Add(permission);




                //var readWriteParticipantRole = PermissionRole.Get(_realm, "readWriteParticipant");
                var permissionUser = PermissionUser.Get(_realm, "88b8e0d78e966e78818751e4dd3dd002");
                var semiPermission = Permission.Get<Note>(permissionUser.Role, _realm);
                semiPermission.CanRead = true;
                semiPermission.CanUpdate = true;
                semiPermission.CanQuery = true;

                note.Permissions.Add(semiPermission);

                _realm.Add(note);
            });
        }

        private void AddNote()
        {
            _realm.Write(() =>
            {
                var guid = Guid.NewGuid().ToString();

                var note = new Note
                {
                    Id = guid,
                    Title = $"Test {guid.Substring(0, 4)}"
                };

                var user = PermissionUser.Get(_realm, User.Current.Identity);

                var permission = Permission.Get<Note>(user.Role, _realm);

                permission.CanCreate = true;
                permission.CanDelete = true;
                permission.CanRead = true;
                permission.CanSetPermissions = true;
                permission.CanUpdate = true;
                permission.CanQuery = true;

                note.Permissions.Add(permission);

                _realm.Add(note);
            });
        }

        public void RemoveNote(NoteViewModel noteViewModel)
        {
            //var realm = Realm.GetInstance(_configuration);
            _realm.Write(() =>
            {
                _realm.Remove(noteViewModel.Note);
            });

            //var noteRo = realm.Find<Note>(noteViewModel.Note.Id);
        }

        private List<ListGroup> CreateGroupedNotes(IEnumerable<Note> notes)
        {
            var grouped = notes.GroupBy(x => new DateTime(x.DueDate.Year, x.DueDate.Month, 1))
                .OrderByDescending(x => x.Key);

            var groups = new List<ListGroup>();

            foreach (var group in grouped)
            {
                var newGroup = new ListGroup($"{group.Key.Month} {group.Key.Year}");
                newGroup.AddRange(group.OrderByDescending(x => x.DueDate).Select(x => new NoteViewModel(x)));
                groups.Add(newGroup);
            }

            return groups;
        }
    }

    public class PocSync : IObserver<SyncProgress>
    {
        public void OnCompleted()
        {
            Debug.WriteLine($"Sync COMPLETE");
        }

        public void OnError(Exception error)
        {
            Debug.WriteLine($"SyncError {error?.Message}");
        }

        public void OnNext(SyncProgress value)
        {
            Debug.WriteLine(
                $"{nameof(OnNext)}: {value.TransferredBytes} / {value.TransferableBytes} ({(value.TransferredBytes / value.TransferableBytes) * 100}%)");
        }
    }

    public class NoteViewModel : INotifyPropertyChanged
    {
        private string _imageCount = "1";

        public Note Note { get; }

        public string ImageCount
        {
            get => _imageCount;
            set
            {
                _imageCount = value;
                OnPropertyChanged(nameof(ImageCount));
            }
        }

        public NoteViewModel(Note note)
        {
            Note = note;
        }

        public NoteViewModel()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SyncObserver : IObserver<SyncProgress>
    {
        public void OnCompleted()
        {
            Debug.WriteLine($"{nameof(OnCompleted)}");
        }

        public void OnError(Exception error)
        {

            Debug.WriteLine($"{nameof(OnError)}");
        }

        public void OnNext(SyncProgress value)
        {
            Debug.WriteLine($"{nameof(OnNext)}");
            Debug.WriteLine($"{nameof(OnNext)}: TransferableBytes = {value.TransferableBytes}");
            Debug.WriteLine($"{nameof(OnNext)}: TransferredBytes = {value.TransferredBytes}");
            Debug.WriteLine($"{nameof(OnNext)}: Sync progress = {(value.TransferredBytes / value.TransferableBytes) * 100}%");


        }
    }

    public class ListGroup : List<NoteViewModel>
    {
        public string Title { get; set; }

        public ListGroup(string title)
        {
            Title = title;
        }

        public static IList<NoteViewModel> All { private set; get; }
    }
}

