using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using BokaRealmPoc.Models;
using BokaRealmPoc.Views;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Realms;
using Realms.Sync;
using Xamarin.Forms;

namespace BokaRealmPoc.ViewModels
{
	public class EditNotePageViewModel : ViewModelBase
	{
	    private FullSyncConfiguration _configuration;
	    private Note _note;

	    public Note Note
	    {
	        get => _note;
	        set
	        {
	            SetProperty(ref _note, value);

	            if (value == null) return;

	            Title = value.Title;
	        }
	    }

	    public DelegateCommand SaveCommand { get; set; }
	    public DelegateCommand AddLandCommand { get; set; }

        public EditNotePageViewModel(INavigationService navigationService) : base(navigationService)
	    {
	        SaveCommand = new DelegateCommand(Save);
	        AddLandCommand = new DelegateCommand(AddLand);
	    }

	    private async void AddLand()
	    {
	        await NavigationService.NavigateAsync($"{nameof(AddLandPage)}?noteId={_note.Id}");
	    }

	    private async void Save()
	    {
	        Debug.WriteLine($"Entered {nameof(Save)}"); ;

            var stopwatch = new Stopwatch();
            stopwatch.Start();
	        var realm = Realm.GetInstance(_configuration);
            stopwatch.Stop();
            Debug.WriteLine($"Elpaset time fetching realm: {stopwatch.Elapsed.TotalSeconds}");

	        realm.Write(() => { Note.Title = Title; });

	        await NavigationService.GoBackAsync();
	    }

        public override async void OnNavigatingTo(NavigationParameters parameters)
        {
            var realm = Realm.GetInstance();

            var noteId = (string) parameters["noteId"];

            Note = realm.Find<Note>(noteId);

            Note.PropertyChanged += (sender, args) =>
            {
                if (sender != null && sender.GetType() == typeof(Note))
                {
                    var note = (Note) sender;
                    Note = note;
                }
            };
            
            Title = Note.Title;
        }
    }
}
