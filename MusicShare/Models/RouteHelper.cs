using MusicShare.Views.Util;
using Mapsui.Styles;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace MusicShare.Models
{
    public static class RouteHelper
    {
        //async void SetLoc()
        //{
        //    var loc = await this.FindLastKnownLocation();
        //    if (loc != null)
        //    {
        //        var pin = new Pin();
        //        pin.Position = new Position(loc.Latitude, loc.Longitude);
        //        pin.Label = "last known";
        //        mapView.Pins.Add(pin);
        //    }

        //    var cloc = await this.FindCurrentLocation();
        //    if (cloc != null)
        //    {
        //        var pin = new Pin();
        //        pin.Label = "current";
        //        pin.Position = new Position(cloc.Latitude, cloc.Longitude);
        //        mapView.Pins.Add(pin);
        //    }

        //    var pos = (await Geocoding.GetLocationsAsync("Бассейная 41")).ToArray();

        //    foreach (var item in pos.Take(1))
        //    {
        //        var pin2 = new Pin();
        //        var place = await Geocoding.GetPlacemarksAsync(item);
        //        pin2.Label = place.First().ToString();
        //        pin2.Position = new Position(item.Latitude, item.Longitude);
        //        mapView.Pins.Add(pin2);
        //    }
        //}

        class Grouping<TKey, TItem> : IGrouping<TKey, TItem>
        {
            public TKey Key { get; }
            public IEnumerable<TItem> Items { get; }

            public Grouping(TKey key, IEnumerable<TItem> items)
            {
                this.Key = key;
                this.Items = items;
            }

            public IEnumerator<TItem> GetEnumerator() { return this.Items.GetEnumerator(); }

            IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
        }

        public static async Task<IEnumerable<IGrouping<Location, Placemark>>> ResolveAddressToPlaces(string address)
        {
            var locs = await Geocoding.GetLocationsAsync(address);
            var results = new List<IGrouping<Location, Placemark>>();

            foreach (var loc in locs)
            {
                var places = await Geocoding.GetPlacemarksAsync(loc);
                results.Add(new Grouping<Location, Placemark>(loc, places));
            }

            return results;
        }

        public static MapLocInfo ToLocInfo(this Placemark place, Xamarin.Forms.Color color)
        {
            var parts = new[] {
                place.CountryName,
                place.Locality,
                place.SubLocality,
                place.Thoroughfare,
                place.SubThoroughfare,
            };

            var text = string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            return new MapLocInfo(color, text, place.Location.Latitude, place.Location.Longitude);
        }

        public static async Task<IEnumerable<Location>> ResolveAddressToLocations(string address)
        {
            return await Geocoding.GetLocationsAsync(address);
        }

        public static async Task<IEnumerable<Placemark>> ResolveLocationToPlaces(Location loc)
        {
            return await Geocoding.GetPlacemarksAsync(loc);
        }

        public static async Task<IEnumerable<Location>> FindRoute(Location from, Location to)
        {
            // https://wiki.openstreetmap.org/wiki/YOURS#API_documentation

            var url = $"http://www.yournavigation.org/api/1.0/gosmore.php?format=geojson&flat={from.Latitude}&flon={from.Longitude}&tlat={to.Latitude}&tlon={to.Longitude}&v=motorcar&fast=1&layer=mapnik";

            try
            {
                var wc = new WebClient();
                var resp = await wc.DownloadStringTaskAsync(url);
                var route = JsonConvert.DeserializeObject<GeoJsonNoProps>(resp);

                IEnumerable<Location> MakeResult()
                {
                    foreach (var item in route.coordinates)
                        yield return new Location(item[1], item[0]);
                }

                return route.coordinates != null && route.coordinates.Length > 0 ? MakeResult() : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                return null;
            }
        }

        public class GeoJsonNoProps
        {
            public string type { get; set; }
            public double[][] coordinates { get; set; }
        }

        public class GeoJson
        {
            public string type { get; set; }
            public double[][] coordinates { get; set; }
            public GeoJsonProps properties { get; set; }
        }

        public class GeoJsonProps
        {
            public string description { get; set; }
            public double distance { get; set; }
            public long traveltime { get; set; }
        }

        public static async Task<Location> FindLastKnownLocation()
        {
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location != null)
                {
                    Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");
                }

                return location;
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Handle not supported on device exception
            }
            catch (FeatureNotEnabledException fneEx)
            {
                // Handle not enabled on device exception
            }
            catch (PermissionException pEx)
            {
                // Handle permission exception
            }
            catch (Exception ex)
            {
                // Unable to get location
            }
            return null;
        }

        public static async Task<Location> FindCurrentLocation()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                var location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");
                }

                return location;
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Handle not supported on device exception
            }
            catch (FeatureNotEnabledException fneEx)
            {
                // Handle not enabled on device exception
            }
            catch (PermissionException pEx)
            {
                // Handle permission exception
            }
            catch (Exception ex)
            {
                // Unable to get location
            }
            return null;
        }
    }
}
