using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace atsenterapp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MinyanMiniView : ContentView
    {
        public int id = -1;
        public Minyan owner;
        public int count;
        public TimeSpan time;
        public MinyanPage page = null;
        public bool host = false;
        public MinyanMiniView(Minyan properties)
        {
            InitializeComponent();
            owner = properties;
            page = new MinyanPage(owner, owner.id, owner.type, owner.Count, owner.address, owner.locationDescription, owner.Distance, owner.Time);
            UpdateDetails();
        }

        public void UpdateDetails()
        {
            if (id == -1)
            {
                id = owner.id;
                string typestring = Global.TfilaToString(owner.type);
                titleLbl.Text = "המניין שלך ל" + typestring;
            }
            count = owner.Count;
            time = owner.Time;
            string tomorrow = time <= DateTime.Now.TimeOfDay ? "מחר" : "מתחיל";
            detailsLbl.Text = string.Format(count.ToString() + " מתפללים  •  {0} בשעה " + time.ToString().Remove(5), tomorrow);
            try
            {
                distanceDisplay.Text = Global.GetDistanceDisplayText(owner.Distance) + " ק\"מ\nממך";
            }
            catch { }
            page.Host = host;
            page.UpdateDetails(count, owner.address, owner.locationDescription, owner.Distance, time);
        }

        public void UpdateDetails(List<string> updates)
        {
            UpdateDetails();
            int updatesCount = updates.Count;
            foreach(var update in updates)
            {
                page.AddUpdate(update, update.Contains("שים לב"));
            }
            if (updatesCount > 0) 
            {
                char symbol = '❶';
                updatesLbl.Text = ((char)(symbol + ((updatesCount >= 11) ? 9 : updatesCount-1))).ToString();
                if (updatesCount > 10)
                    updatesLbl.Text += '+';
                
                updatesLbl.IsVisible = true;
            }
        }

        public void Button_Clicked(object sender, EventArgs e)
        {
            Open();
        }

        public void Open()
        {
            try
            {
                Navigation.PushModalAsync(page);
            }
            catch { }
            updatesLbl.IsVisible = false;
        }
    }
}