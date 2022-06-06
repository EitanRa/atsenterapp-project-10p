using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

namespace atsenterapp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LocationPickerPage : ContentPage
    {
        private Pin mapPin;
        public event EventHandler<Location> LocationPicked;
        public double maxDistance = -1;
        private Location startingLocation = null;
        public LocationPickerPage(Location pinLoc = null, double maxDistance = -1)
        {
            InitializeComponent();
            cancelBtn.BackgroundColor = Color.WhiteSmoke;
            cancelBtn.TextColor = Color.Black;
            if (pinLoc == null)
                pinLoc = Global.MainPage.deviceLocation;
            Position initMapPosition = new Position(pinLoc.Latitude, pinLoc.Longitude);
            mapPin = new Pin
            {
                Label = "המיקום הנבחר",
                Position = initMapPosition,
                Type = PinType.Generic
            };
            map.Pins.Add(mapPin);
            map.MoveToRegion(new MapSpan(initMapPosition, 0.01, 0.01));
            if (maxDistance > 0)
            {
                this.maxDistance = maxDistance;
                startingLocation = pinLoc;
            }
        }

        private async void currentLocBtn_Clicked(object sender, EventArgs e)
        {
            currentLocBtn.IsEnabled = false;
            currentLocBtn.Text = "המתן...";
            try
            {
                Location currentLoc = await Geolocation.GetLocationAsync();
                if (CheckDistance(currentLoc))
                {
                    var handler = LocationPicked;
                    handler?.Invoke(this, currentLoc);
                    ClosePage();
                }
            }
            catch
            {
                Global.ShowMessage("שגיאת GPS", "מצטערים, נראה שאין קליטת GPS. בחר נקודה מהמפה", "הבנתי");
            }
        }

        private void cancelBtn_Clicked(object sender, EventArgs e)
        {
            var handler = LocationPicked;
            handler?.Invoke(this, null);
            ClosePage();
        }

        private void okBtn_Clicked(object sender, EventArgs e)
        {
            Location mapLoc = new Location(mapPin.Position.Latitude, mapPin.Position.Longitude);
            if (CheckDistance(mapLoc))
            {
                var handler = LocationPicked;
                handler?.Invoke(this, mapLoc);
                ClosePage();
            }
        }

        private void map_MapClicked(object sender, MapClickedEventArgs e)
        {
            mapPin.Position = e.Position;
        }

        private async void ClosePage()
        {
            try
            {
                await Navigation.PopModalAsync();
            }
            catch
            {
                Global.PopText("לא ניתן לסגור את חלון בחירת המיקום");
            }
        }

        private bool CheckDistance(Location location, bool showErrMsg = true)
        {
            if (maxDistance > 0)
            {
                double distance = location.CalculateDistance(startingLocation, DistanceUnits.Kilometers);
                bool legal = distance <= maxDistance;
                if (!legal)
                    Global.ShowMessage("טווח לא תקין", "המיקום שבחרת נמצא במרחק של " + (int)distance + " ק\"מ מהמיקום המוגדר. בחר מיקום בטווח של עד " + (int)maxDistance + " ק\"מ", "אישור");
                return legal;
            }
            else
                return true;
        }
    }
}