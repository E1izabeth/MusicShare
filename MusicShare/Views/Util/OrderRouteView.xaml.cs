//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//using Mapsui;
//using Mapsui.Layers;
//using Mapsui.Projection;
//using Mapsui.UI.Forms;
//using Mapsui.Widgets;
//using Mapsui.Widgets.ScaleBar;
//using Xamarin.Essentials;
//using BruTile.Web;
//using BruTile.Predefined;

//using Xamarin.Forms;
//using Xamarin.Forms.Xaml;
//using System.Net;
//using Newtonsoft.Json;
//using Mapsui.Geometries;
//using System.Collections.ObjectModel;
//using System.Collections.Specialized;
//using System.Windows.Input;
//using MusicShare.Models;

//namespace MusicShare.Views.Util
//{
//    [DesignTimeVisible(false)]
//    public partial class OrderRouteView : Grid
//    {
//        #region ObservableCollection<MapLocInfo> MapLocations 

//        public ObservableCollection<MapLocInfo> MapLocations
//        {
//            get { return (ObservableCollection<MapLocInfo>)this.GetValue(MapLocationsProperty); }
//            set { this.SetValue(MapLocationsProperty, value); }
//        }

//        // Using a BindableProperty as the backing store for MapLocations. This enables animation, styling, binding, etc...
//        public static readonly BindableProperty MapLocationsProperty =
//            BindableProperty.Create("MapLocations", typeof(ObservableCollection<MapLocInfo>), typeof(OrderRouteView), default(ObservableCollection<MapLocInfo>),
//                propertyChanged: CollectionBindingWatcher.For<OrderRouteView>(c => c._mapLocsCollectionBindingContext));

//        #endregion

//        #region ObservableCollection<MapRouteInfo> MapRoutes 

//        public ObservableCollection<MapRouteInfo> MapRoutes
//        {
//            get { return (ObservableCollection<MapRouteInfo>)this.GetValue(MapRoutesProperty); }
//            set { this.SetValue(MapRoutesProperty, value); }
//        }

//        // Using a BindableProperty as the backing store for MapRoutes. This enables animation, styling, binding, etc...
//        public static readonly BindableProperty MapRoutesProperty =
//            BindableProperty.Create("MapRoutes", typeof(ObservableCollection<MapRouteInfo>), typeof(OrderRouteView), default(ObservableCollection<MapRouteInfo>),
//                propertyChanged: CollectionBindingWatcher.For<OrderRouteView>(c => c._mapRoutesCollectionBindingContext));

//        #endregion

//        #region MapLocInfo SelectedMapLocation 

//        public MapLocInfo SelectedMapLocation
//        {
//            get { return (MapLocInfo)this.GetValue(SelectedMapLocationProperty); }
//            set { this.SetValue(SelectedMapLocationProperty, value); }
//        }

//        // Using a BindableProperty as the backing store for SelectedMapLocation. This enables animation, styling, binding, etc...
//        public static readonly BindableProperty SelectedMapLocationProperty =
//            BindableProperty.Create("SelectedMapLocation", typeof(MapLocInfo), typeof(OrderRouteView), default(MapLocInfo));

//        #endregion

//        #region ICommand BringMapLocsIntoViewMapCommand  

//        public ICommand BringMapLocsIntoViewMapCommand
//        {
//            get { return (ICommand)this.GetValue(BringMapLocsIntoViewMapCommandProperty); }
//            private set { this.SetValue(BringMapLocsIntoViewMapCommandProperty, value); }
//        }

//        // Using a BindableProperty as the backing store for BringMapLocsIntoViewMapCommand . This enables animation, styling, binding, etc...
//        public static readonly BindableProperty BringMapLocsIntoViewMapCommandProperty =
//            BindableProperty.Create("BringMapLocsIntoViewMapCommand", typeof(ICommand), typeof(OrderRouteView), default(ICommand));

//        #endregion

//        #region ICommand BringMapRoutesIntoViewMapCommand 

//        public ICommand BringMapRoutesIntoViewMapCommand
//        {
//            get { return (ICommand)this.GetValue(BringMapRoutesIntoViewMapCommandProperty); }
//            private set { this.SetValue(BringMapRoutesIntoViewMapCommandProperty, value); }
//        }

//        // Using a BindableProperty as the backing store for BringMapRoutesIntoViewMapCommand. This enables animation, styling, binding, etc...
//        public static readonly BindableProperty BringMapRoutesIntoViewMapCommandProperty =
//            BindableProperty.Create("BringMapRoutesIntoViewMapCommand", typeof(ICommand), typeof(OrderRouteView), default(ICommand));

//        #endregion

//        #region ICommand BringMapPositionIntoViewMapCommand 

//        public ICommand BringMapPositionIntoViewMapCommand
//        {
//            get { return (ICommand)this.GetValue(BringMapPositionIntoViewMapCommandProperty); }
//            private set { this.SetValue(BringMapPositionIntoViewMapCommandProperty, value); }
//        }

//        // Using a BindableProperty as the backing store for BringMapPositionIntoViewMapCommand. This enables animation, styling, binding, etc...
//        public static readonly BindableProperty BringMapPositionIntoViewMapCommandProperty =
//            BindableProperty.Create("BringMapPositionIntoViewMapCommand", typeof(ICommand), typeof(OrderRouteView), default(ICommand));

//        #endregion

//        #region ICommand OnMapClickedCommand 

//        public ICommand OnMapClickedCommand
//        {
//            get { return (ICommand)this.GetValue(OnMapClickedCommandProperty); }
//            set { this.SetValue(OnMapClickedCommandProperty, value); }
//        }

//        // Using a BindableProperty as the backing store for OnMapClickedCommand. This enables animation, styling, binding, etc...
//        public static readonly BindableProperty OnMapClickedCommandProperty =
//            BindableProperty.Create("OnMapClickedCommand", typeof(ICommand), typeof(OrderRouteView), default(ICommand));

//        #endregion

//        #region ICommand OnMapLongClickedCommand 

//        public ICommand OnMapLongClickedCommand
//        {
//            get { return (ICommand)this.GetValue(OnMapLongClickedCommandProperty); }
//            set { this.SetValue(OnMapLongClickedCommandProperty, value); }
//        }

//        // Using a BindableProperty as the backing store for OnMapLongClickedCommand. This enables animation, styling, binding, etc...
//        public static readonly BindableProperty OnMapLongClickedCommandProperty =
//            BindableProperty.Create("OnMapLongClickedCommand", typeof(ICommand), typeof(OrderRouteView), default(ICommand));

//        #endregion

//        #region ICommand OnMapLocSelectedCommand 

//        public ICommand OnMapLocSelectedCommand
//        {
//            get { return (ICommand)this.GetValue(OnMapLocSelectedCommandProperty); }
//            set { this.SetValue(OnMapLocSelectedCommandProperty, value); }
//        }

//        // Using a BindableProperty as the backing store for OnMapLocSelectedCommand. This enables animation, styling, binding, etc...
//        public static readonly BindableProperty OnMapLocSelectedCommandProperty =
//            BindableProperty.Create("OnMapLocSelectedCommand", typeof(ICommand), typeof(OrderRouteView), default(ICommand));

//        #endregion

//        #region ICommand OnMapLocationsChangedCommand 

//        public ICommand OnMapLocationsChangedCommand
//        {
//            get { return (ICommand)this.GetValue(OnMapLocationsChangedCommandProperty); }
//            set { this.SetValue(OnMapLocationsChangedCommandProperty, value); }
//        }

//        // Using a BindableProperty as the backing store for OnMapLocationsChangedCommand. This enables animation, styling, binding, etc...
//        public static readonly BindableProperty OnMapLocationsChangedCommandProperty =
//            BindableProperty.Create("OnMapLocationsChangedCommand", typeof(ICommand), typeof(OrderRouteView), default(ICommand));

//        #endregion

//        private CollectionBindingContext _mapLocsCollectionBindingContext;
//        private CollectionBindingContext _mapRoutesCollectionBindingContext;

//        public OrderRouteView()
//        {
//            _mapLocsCollectionBindingContext = new CollectionBindingContext();
//            _mapLocsCollectionBindingContext.OnCollectionChanged += this.OnMapLocsCollectionChanged;

//            _mapRoutesCollectionBindingContext = new CollectionBindingContext();
//            _mapRoutesCollectionBindingContext.OnCollectionChanged += this.OnMapRoutesCollectionChanged;

//            this.InitializeComponent();

//            var map = new Mapsui.Map
//            {
//                CRS = "EPSG:3857",
//                Transformation = new MinimalTransformation(),
//            };

//            var tileSource = new HttpTileSource(
//                new GlobalSphericalMercator(),
//                "http://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
//                new[] { "a", "b", "c" },
//                name: "OpenStreetMap",
//                attribution: new BruTile.Attribution("© OpenStreetMap contributors", "http://www.openstreetmap.org/copyright")
//            );
//            var tileLayer = new TileLayer(tileSource) { Name = "OpenStreetMap" };

//            map.Layers.Add(tileLayer);
//            //map.Widgets.Add(new ScaleBarWidget(map)
//            //{
//            //    TextAlignment = Alignment.Center,
//            //    HorizontalAlignment = HorizontalAlignment.Left,
//            //    VerticalAlignment = VerticalAlignment.Bottom
//            //});

//            //var marker = new MapMarker(this, new GeoCoordinate(lat, long), MapMarkerAlignmentType.CenterBottom, bitmap);
//            //mapView.AddMarker(marker);

//            mapView.Map = map;
//            //mapView.PinClicked += (sender, ea) =>
//            //{
//            //    mapView.SelectedPin = ea.Pin;
//            //};
//            mapView.SelectedPinChanged += (sender, ea) =>
//            {
//                this.SelectedMapLocation = ea.SelectedPin?.Tag as MapLocInfo;
//                this.OnMapLocSelectedCommand?.Execute(this.SelectedMapLocation);
//            };

//            mapView.MapClicked += (sender, ea) =>
//            {
//                this.OnMapClickedCommand?.Execute(ea.Point);
//            };

//            mapView.MapLongClicked += (sender, ea) =>
//            {
//                this.OnMapLongClickedCommand?.Execute(ea.Point);
//            };

//            mapView.IsMyLocationButtonVisible = true;
//            mapView.IsZoomButtonVisible = true;
//            mapView.IsNorthingButtonVisible = true;
//            mapView.MyLocationEnabled = true;

//            this.BringMapLocsIntoViewMapCommand = new Command(() => this.InvokeAction(() =>
//            {
//                if (mapView.Pins.Count > 0)
//                {
//                    var llat = mapView.Pins.Min(p => p.Position.Latitude);
//                    var llon = mapView.Pins.Min(p => p.Position.Longitude);
//                    var hlat = mapView.Pins.Max(p => p.Position.Latitude);
//                    var hlon = mapView.Pins.Max(p => p.Position.Longitude);
//                    var lp = SphericalMercator.FromLonLat(llon, llat);
//                    var hp = SphericalMercator.FromLonLat(hlon, hlat);
//                    var bbox = new BoundingBox(lp, hp);
//                    mapView.Navigator.NavigateTo(bbox);
//                }
//            }));

//            this.BringMapRoutesIntoViewMapCommand = new Command(() => this.InvokeAction(() =>
//            {
//                var allPositions = mapView.Drawables.OfType<Polyline>().SelectMany(l => l.Positions).ToList();
//                if (allPositions.Count > 0)
//                {
//                    var llat = allPositions.Min(p => p.Latitude);
//                    var llon = allPositions.Min(p => p.Longitude);
//                    var hlat = allPositions.Max(p => p.Latitude);
//                    var hlon = allPositions.Max(p => p.Longitude);
//                    var lp = SphericalMercator.FromLonLat(llon, llat);
//                    var hp = SphericalMercator.FromLonLat(hlon, hlat);
//                    var bbox = new BoundingBox(lp, hp);
//                    mapView.Navigator.NavigateTo(bbox);
//                }
//            }));

//            this.BringMapPositionIntoViewMapCommand = new Command(o => this.InvokeAction(() =>
//            {
//                if (o is Location loc)
//                {
//                    var pp = SphericalMercator.FromLonLat(loc.Longitude, loc.Latitude);
//                    var bbox = new BoundingBox(pp, pp);
//                    mapView.Navigator.NavigateTo(bbox);
//                }
//            }));

//            this.UpdateMyLocation();
//        }

//        private async void UpdateMyLocation()
//        {
//            var loc = await RouteHelper.FindCurrentLocation();
//            if (loc == null)
//                loc = await RouteHelper.FindLastKnownLocation();

//            if (loc != null)
//                mapView.MyLocationLayer.UpdateMyLocation(new Position(loc.Latitude, loc.Longitude));
//        }

//        private void OnMapLocsCollectionChanged(INotifyCollectionChanged locs, NotifyCollectionChangedEventArgs e)
//        {
//            if (e.Action == NotifyCollectionChangedAction.Remove ||
//                e.Action == NotifyCollectionChangedAction.Replace)
//            {
//                foreach (MapLocInfo item in e.OldItems)
//                {
//                    var index = mapView.Pins.IndexOf(p => p.Tag == item);
//                    if (index >= 0)
//                        mapView.Pins.RemoveAt(index);
//                }
//            }

//            if (e.Action == NotifyCollectionChangedAction.Add ||
//                e.Action == NotifyCollectionChangedAction.Replace)
//            {
//                foreach (MapLocInfo item in e.NewItems)
//                {
//                    var pin = new Pin();
//                    pin.Position = new Position(item.Location.Latitude, item.Location.Longitude);
//                    pin.Label = item.Label;
//                    pin.Color = item.Color;
//                    pin.Tag = item;
//                    //pin.BindingContext = item;
//                    //pin.SetBinding(Pin.ColorProperty, "Color");
//                    mapView.Pins.Add(pin);
//                }
//            }

//            if (e.Action == NotifyCollectionChangedAction.Reset)
//            {
//                mapView.Pins.Clear();
//            }

//            this.OnMapLocationsChangedCommand?.Execute(null);
//        }

//        private void OnMapRoutesCollectionChanged(INotifyCollectionChanged routes, NotifyCollectionChangedEventArgs e)
//        {
//            if (e.Action == NotifyCollectionChangedAction.Remove ||
//                e.Action == NotifyCollectionChangedAction.Replace)
//            {
//                foreach (MapRouteInfo item in e.OldItems)
//                {
//                    var index = mapView.Drawables.IndexOf(p => p.Tag == item);
//                    if (index >= 0)
//                        mapView.Drawables.RemoveAt(index);
//                }
//            }

//            if (e.Action == NotifyCollectionChangedAction.Add ||
//                e.Action == NotifyCollectionChangedAction.Replace)
//            {
//                foreach (MapRouteInfo route in e.NewItems)
//                {
//                    var polyline = new Polyline();
//                    polyline.StrokeWidth = route.StrokeWidth;
//                    polyline.StrokeColor = route.Color;
//                    foreach (var loc in route.Locs)
//                        polyline.Positions.Add(new Position(loc.Latitude, loc.Longitude));

//                    polyline.Tag = route;
//                    mapView.Drawables.Add(polyline);
//                }
//            }

//            if (e.Action == NotifyCollectionChangedAction.Reset)
//            {
//                mapView.Drawables.Clear();
//            }
//        }
//    }

//    public class MapLocInfo
//    {
//        public Color Color { get; }
//        public string Label { get; }

//        public Location Location { get; }

//        public MapLocInfo(Color color, string title, double posLatitude, double posLongitude)
//        {
//            this.Color = color;
//            this.Label = title;
//            this.Location = new Location(posLatitude, posLongitude);
//        }
//    }

//    public class MapRouteInfo
//    {
//        public Color Color { get; }
//        public float StrokeWidth { get; }
//        public List<Location> Locs { get; }

//        public MapRouteInfo(Color color, float strokeWidth, List<Location> locs)
//        {
//            this.Color = color;
//            this.StrokeWidth = strokeWidth;
//            this.Locs = locs;
//        }
//    }
//}