using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace LocalFileDB.Services
{
    /// <summary>
    /// Requests thumbnails from the Windows Shell and converts them to WPF bitmaps.
    /// </summary>
    internal static class WindowsThumbnailProvider
    {
        private static readonly Guid ShellItemImageFactoryId = new("BCC18B79-BA16-442F-80C4-8A59C30C463B");
        private const int ThumbnailOnly = 0x00000008;

        /// <summary>
        /// Returns the Shell thumbnail for a file, or null when no thumbnail is available.
        /// </summary>
        public static BitmapSource? TryGetThumbnail(string filePath, int width, int height)
        {
            IShellItemImageFactory? imageFactory = null;
            IntPtr bitmapHandle = IntPtr.Zero;

            try
            {
                var interfaceId = ShellItemImageFactoryId;
                var result = SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref interfaceId, out imageFactory);
                if (result < 0 || imageFactory is null)
                {
                    return null;
                }

                result = imageFactory.GetImage(new NativeSize(width, height), ThumbnailOnly, out bitmapHandle);
                if (result < 0 || bitmapHandle == IntPtr.Zero)
                {
                    return null;
                }

                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    bitmapHandle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bitmapSource.Freeze();
                return bitmapSource;
            }
            finally
            {
                if (bitmapHandle != IntPtr.Zero)
                {
                    DeleteObject(bitmapHandle);
                }

                if (imageFactory is not null && Marshal.IsComObject(imageFactory))
                {
                    Marshal.ReleaseComObject(imageFactory);
                }
            }
        }

        /// <summary>
        /// Creates a Shell item for the supplied path and requested COM interface.
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateItemFromParsingName(
            string path,
            IntPtr bindContext,
            ref Guid interfaceId,
            out IShellItemImageFactory imageFactory);

        /// <summary>
        /// Releases the native bitmap handle returned by Windows Shell.
        /// </summary>
        [DllImport("gdi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr handle);

        /// <summary>
        /// Defines the Windows Shell interface used to retrieve an item's thumbnail.
        /// </summary>
        [ComImport]
        [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            /// <summary>
            /// Retrieves a thumbnail bitmap for the Shell item.
            /// </summary>
            [PreserveSig]
            int GetImage(NativeSize size, int flags, out IntPtr bitmapHandle);
        }

        /// <summary>
        /// Matches the native Windows SIZE structure used by the Shell interface.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct NativeSize
        {
            /// <summary>
            /// Creates the requested thumbnail dimensions.
            /// </summary>
            public NativeSize(int width, int height)
            {
                Width = width;
                Height = height;
            }

            /// <summary>
            /// Gets the requested width.
            /// </summary>
            public readonly int Width;

            /// <summary>
            /// Gets the requested height.
            /// </summary>
            public readonly int Height;
        }
    }
}
