using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DirectPaymentPage : ContentPage
    {
        public DirectPaymentPage()
        {
            InitializeComponent();
        }

        public string ShopNumber { get; internal set; }
    }
}