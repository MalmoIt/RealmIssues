using BokaRealmPoc.ViewModels;
using Xamarin.Forms;

namespace BokaRealmPoc.Views
{
    public partial class AddLandPage : ContentPage
    {
        public AddLandPage()
        {
            InitializeComponent();
        }

        private void OnLandSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var listview = (ListView) sender;
            if (listview == null || listview.SelectedItem == null) return;

            var vm = (AddLandPageViewModel) BindingContext;
            vm.SelectedLand((LandNoteViewModel) e.SelectedItem);

            listview.SelectedItem = null;
        }
    }
}
