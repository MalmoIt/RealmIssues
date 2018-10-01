using System;
using System.Diagnostics;
using BokaRealmPoc.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Realms;
using Realms.Sync;

namespace BokaRealmPoc.ViewModels
{
	public class EditNotePageViewModel : ViewModelBase
	{
	    private FullSyncConfiguration _configuration;
	    private Note _note;

	    public Note Note
	    {
	        get => _note;
	        set => SetProperty(ref _note, value);
	    }

	    public DelegateCommand SaveCommand { get; set; }

	    public EditNotePageViewModel(INavigationService navigationService) : base(navigationService)
	    {
	        _configuration = new FullSyncConfiguration(new Uri("/~/nordmannTwo", UriKind.Relative));

	        SaveCommand = new DelegateCommand(Save);

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
            var realm = await Realm.GetInstanceAsync(_configuration);

            var noteId = (string) parameters["noteId"];

            Note = realm.Find<Note>(noteId);

            Title = Note.Title;
        }
    }
}
