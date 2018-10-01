using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BokaRealmPoc.ViewModels;
using Xamarin.Forms;

namespace BokaRealmPoc.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OnDeleteTapped(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);

            var vm = (MainPageViewModel) BindingContext;
            vm?.RemoveNote((NoteViewModel) mi.CommandParameter);
        }
    }
}