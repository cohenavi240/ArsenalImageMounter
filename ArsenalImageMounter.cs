using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class ArsenalImageMounter
    {
        public ArsenalImageMounter()
        {
            
        }

        public bool IsMountable(string imageFilePath, ImageType imageType = ImageType.Raw)
        {
            if (!File.Exists(imageFilePath))
            {
                // log
                return false;
            }

            string extensions = Path.GetExtension(imageFilePath);
            if (extensions == null)
            {
                // log
                return false;
            }

            return ArsenalMounter.IsMountable(extensions, imageType);
        }

        public MountedDevice Mount(string imageFilePath, ImageType imageType = ImageType.Raw, MediaAccess accessMode = MediaAccess.ReadOnly, bool fakeMBR = false)
        {
            try
            {
                // log
                var mountedDevice = ArsenalMounter.Mount(imageFilePath, imageType, accessMode, fakeMBR);
                // log
                return mountedDevice;
            }
            catch (Exception)
            {
                // log
                throw;
            }
        }

        public void UnmountAll()
        {
            ArsenalMounter.UnmountAll();
        }

        public void UnmonutDevice(MountedDevice mountedDevice)
        {
            ArsenalMounter.UnmountDevice(mountedDevice);
        }
    }
}
