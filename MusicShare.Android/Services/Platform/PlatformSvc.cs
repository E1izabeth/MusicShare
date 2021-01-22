using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;
using MusicShare.Droid.Services.Bluetooth;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.Platform;
using MusicShare.Services.Streams;

namespace MusicShare.Droid.Services.Platform
{
    class PlatformSvc : IPlatformServices
    {
        class FileFsItem : IPlatformFsItem
        {
            public string Name { get { return _file.Name; } }
            public string Path { get { return _file.AbsolutePath; } }
            public bool IsDir { get { return _file.IsDirectory; } }

            readonly Java.IO.File _file;

            public FileFsItem(Java.IO.File file)
            {
                _file = file;
            }

            public IPlatformFsItem[] GetDirs()
            {
                return _file.ListFiles().Where(f => f.IsDirectory).Select(f => new FileFsItem(f)).OrderBy(s => s.Name).ToArray();
            }

            public IPlatformFsItem[] GetFiles()
            {
                return _file.ListFiles().Where(f => !f.IsDirectory).Select(f => new FileFsItem(f)).OrderBy(s => s.Name).ToArray();
            }
        }

        private readonly Service _service;

        public PlatformSvc(Service service)
        {
            PlatformContext.SetInstance(this);
            _service = service;
        }

        public IBluetoothConnectorImpl CreateBluetoothConnector(StreamNegotiator negotiator, Guid serviceId, string localName)
        {
            var result = BluetoothAdapter.DefaultAdapter != null ? new BluetoothConnectorService(negotiator, serviceId, localName, _service)
                                                                      : (IBluetoothConnectorImpl)new NullBluetoothConnectorService();
            return result;
        }

        public void DemandFsPermission()
        {
            
        }

        public IPlatformFsItem[] GetFsRoots()
        {
            // _service.DataDir
            var paths = new[]{
                Android.OS.Environment.ExternalStorageDirectory,
                Android.OS.Environment.RootDirectory,
                Android.OS.Environment.DataDirectory,
            };

            //return System.Environment.GetLogicalDrives().Select(d => {
            //    try { return new DirFsItem(new DirectoryInfo(d)); }
            //    catch { return null; }
            //}).Where(d => d != null).ToArray();

            return paths.Select(p => new FileFsItem(p)).ToArray();
        }

        public string[] PickMusicFiles()
        {
            throw new NotImplementedException();
        }
    }
}