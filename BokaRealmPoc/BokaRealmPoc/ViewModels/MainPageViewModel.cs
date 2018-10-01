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
using Realms;
using Realms.Sync;
using Xamarin.Forms;

namespace BokaRealmPoc.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public int Counter
        {
            get => _counter;
            set => SetProperty(ref _counter, value);
        }

        private bool _isLoading;
        //private IEnumerable<Note> _notes;
        private ObservableCollection<NoteViewModel> _notes = new ObservableCollection<NoteViewModel>();
        //private QueryBasedSyncConfiguration _configuration;
        private IDisposable _token; // TODO Dispose...
        private int _counter;
        private Note _selectedItem;
        private Realm _realm;
        public DelegateCommand AddNoteCommand { get; set; }
        public DelegateCommand AddManyCommand { get; set; }
        public DelegateCommand DeleteAllCommand { get; set; }

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

        public ObservableCollection<NoteViewModel> Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public MainPageViewModel(INavigationService navigationService)
            : base(navigationService)
        {
            AddNoteCommand = new DelegateCommand(AddNote);
            AddManyCommand = new DelegateCommand(AddMany);
            DeleteAllCommand = new DelegateCommand(DeleteAll);
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
            IsLoading = true;

            var user = User.Current;
            //_configuration = new FullSyncConfiguration(new Uri("/~/nordmannTwo", UriKind.Relative), user);
            ////_configuration = new QueryBasedSyncConfiguration(new Uri("/boka-realm", UriKind.Relative));

            var serverURL = new System.Uri("realms://bokapoc.de1a.cloud.realm.io");

            RealmConfiguration.DefaultConfiguration = new QueryBasedSyncConfiguration(user: user);
            
            try
            {
                _realm = Realm.GetInstance();
                _realm.Error += (sender, args) =>
                {
                    var k = args;
                    var l = sender;
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            Notes = new ObservableCollection<NoteViewModel>(_realm.All<Note>().ToList().Select(x => new NoteViewModel(x)));

            IsLoading = false;

            Counter = Notes.Count();
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

        private async void AddMany()
        {            
            _realm.Write(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    var landId = Guid.NewGuid().ToString();
                    var land = new Land
                    {
                        Id = landId,
                        Title = $"Land {landId.Substring(0, 4)}"
                    };

                    var note = new Note
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = $"Test {i}"
                    };

                    note.Lands.Add(land);

                    _realm.Add(note);
                }
            });
        }

        private async void AddNote()
        {
            _realm.Write(() =>
            {
                var guid = Guid.NewGuid().ToString();

                var note = new Note
                {
                    Id = guid,
                    Title = $"Test {guid.Substring(0, 4)}"
                };

                _realm.Add(note);
            });

            Notes = new ObservableCollection<NoteViewModel>(_realm.All<Note>().ToList().Select(x => new NoteViewModel(x)));
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
            Debug.WriteLine($"{nameof(OnNext)}: Sync progress = {(value.TransferredBytes/value.TransferableBytes)*100}%");


        }
    }
}

