using Arsenal.ImageMounter;
using Arsenal.ImageMounter.Devio.Server.Services;
using static Arsenal.ImageMounter.Devio.Server.Interaction.DevioServiceFactory;







namespace ConsoleApp1
{
    public static class ArsenalMounter
    {
        #region Properties

        private static readonly ScsiAdapter ArsenalAdapter = new ScsiAdapter();

        public static IEnumerable<ProviderType> SupportedProxyTypes => new List<ProviderType> 
        { 
            ProviderType.None, 
            ProviderType.MultiPartRaw, 
            ProviderType.DiscUtils
        };

        #endregion

        public static bool IsMountable(string imageFilePath, ImageType imageType = ImageType.Raw)
        {
            ProviderType proxyType = imageType switch
            {
                ImageType.Unknown => TryGetProxyType(imageFilePath),
                _ => GetProxyType(imageType)
            };

            return IsMountable(imageFilePath, imageType);
        }

        public static MountedDevice Mount(string imageFilePath, ImageType imageType = ImageType.Raw, MediaAccess accessMode = MediaAccess.ReadOnly, bool fakeMBR = false)
        {
            if (!IsMountable(imageFilePath, imageType)) return null;

            try
            {
                DevioServiceBase devioService = MountDevice(imageFilePath, imageType, accessMode, fakeMBR);
                var diskDevice = ArsenalAdapter.OpenDevice(devioService.DiskDeviceNumber);

                MountedDevice mountedDevice = new()
                {
                    DeviceNumber = diskDevice.DeviceNumber,
                    DevicePath = diskDevice.DevicePath.Replace("?", "."),
                    DiskId = diskDevice.DiskId,
                    DiskSize = devioService.DiskSize,
                    Description = devioService.Description,
                    Available = diskDevice.DiskPolicyOffline.HasValue && !diskDevice.DiskPolicyOffline.Value
                };

                return mountedDevice;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static MountedDevice Mount(string imageFilePath, 
            ProviderType providerType, 
            bool fakeMBR)
        {
            DevioServiceBase devioService = MountDevice(imageFilePath, providerType, fakeMBR);

            var diskDevice = ArsenalAdapter.OpenDevice(devioService.DiskDeviceNumber);

            MountedDevice mountedDevice = new()
            {
                DeviceNumber = diskDevice.DeviceNumber,
                DevicePath = diskDevice.DevicePath.Replace("?", "."),
                DiskId = diskDevice.DiskId,
                DiskSize = devioService.DiskSize,
                Description = devioService.Description,
                Available = diskDevice.DiskPolicyOffline.HasValue && !diskDevice.DiskPolicyOffline.Value
            };

            return mountedDevice;
        }

        public static DevioServiceBase MountDevice(string imageFilePath,
            ProviderType providerType,
            bool fakeMBR)
        {
            DevioServiceBase devioServiceBase = GetService(imageFilePath, VirtualDiskAccess.ReadOnly, providerType, fakeMBR);
            DeviceFlags deviceFlag = GetDeviceFlags(providerType, MediaAccess.ReadOnly);
            devioServiceBase.StartServiceThreadAndMount(ArsenalAdapter, deviceFlag);

            Thread.Sleep(1000);

            return devioServiceBase;
        }

        public static void UnmountAll()
        {
            if (ArsenalAdapter.GetDeviceList().Length > 0)
            {
                ArsenalAdapter.RemoveAllDevicesSafe();
            }
        }

        public static void UnmountDevice(MountedDevice mountedDevice)
        {
            if (!ArsenalAdapter.GetDeviceList().Contains(mountedDevice.DeviceNumber))
            {
                throw new Exception("Unmounted: Device Not Found");
            }

            ArsenalAdapter.RemoveDeviceSafe(mountedDevice.DeviceNumber);
        }

        public static void Dispose()
        {
            ArsenalAdapter.RemoveAllDevices();
            ArsenalAdapter.Dispose();
        }

        #region Private Methods

        private static DeviceFlags GetDeviceFlags(ProviderType proxyType, MediaAccess accessMode)
        {
            if (proxyType == ProviderType.DiscUtils)
            {
                switch (accessMode)
                {
                    case MediaAccess.ReadOnly:
                        return DeviceFlags.FakeDiskSignatureIfZero | DeviceFlags.ReadOnly;
                }
            }
            else if (proxyType == ProviderType.MultiPartRaw)
            {
                switch (accessMode)
                {
                    case MediaAccess.ReadOnly:
                        return DeviceFlags.FakeDiskSignatureIfZero | DeviceFlags.ReadOnly;
                }
            }

            return DeviceFlags.None;
        }

        private static DevioServiceBase MountDevice(string imageFilePath, 
            ImageType imageType, 
            MediaAccess accessMode, 
            bool fakeMBR = false)
        {
            ProviderType proxyType = imageType switch
            {
                ImageType.Unknown => TryGetProxyType(imageFilePath),
                _ => GetProxyType(imageType)
            };


            DevioServiceBase devioService = GetService(imageFilePath, GetVirtualDiscAccess(accessMode), proxyType, fakeMBR);
            DeviceFlags deviceFlag = DeviceFlags.FakeDiskSignatureIfZero | DeviceFlags.ReadOnly;
            devioService.StartServiceThreadAndMount(ArsenalAdapter, deviceFlag);

            Thread.Sleep(1000);

            return devioService;
        }

        private static ProviderType TryGetProxyType(string imageFilePath)
        {
            foreach (var proxyType in SupportedProxyTypes)
            {
                try
                {
                    using (GetService(imageFilePath, FileAccess.Read, proxyType))
                        return proxyType;
                }
                catch (Exception)
                {
                    //
                }
            }

            throw new Exception($"Get mounted device for image {imageFilePath} failed");
        }

        private static VirtualDiskAccess GetVirtualDiscAccess(MediaAccess accessMode)
        {
            return accessMode switch
            {
                MediaAccess.ReadOnly => VirtualDiskAccess.ReadOnly,
                MediaAccess.ReadOnlyFileSystem => VirtualDiskAccess.ReadOnlyFileSystem,
                MediaAccess.ReadWriteFileSystem => VirtualDiskAccess.ReadWriteFileSystem,
                MediaAccess.ReadWriteOriginal => VirtualDiskAccess.ReadWriteOriginal,
                MediaAccess.ReadWriteOverlay => VirtualDiskAccess.ReadWriteOverlay,
                _ => VirtualDiskAccess.ReadOnly
            };
        }

        private static ProviderType GetProxyType(ImageType imageType)
        {
            return imageType switch
            {
                ImageType.DiscUtils => ProviderType.DiscUtils,
                ImageType.MultiPartRaw => ProviderType.MultiPartRaw,
                ImageType.Raw => ProviderType.None,
                _ => ProviderType.None
            };
        }

        private static bool IsMountable(string imageFilePath, ProviderType proxyType)
        {
            try
            {
                using var service = GetService(imageFilePath, FileAccess.Read, proxyType);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}