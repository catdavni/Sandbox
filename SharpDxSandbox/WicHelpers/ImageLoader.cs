﻿using SharpDX.IO;

namespace SharpDxSandbox.WicHelpers;

internal static class ImageLoader
{
    public static SharpDX.WIC.Bitmap Load(string path)
    {
        using var imageFactory = new SharpDX.WIC.ImagingFactory2();
        using var fileStream = new SharpDX.WIC.WICStream(imageFactory, path, NativeFileAccess.Read);
        // What's the difference between WICStream and NativeFileStream
        //using NativeFileStream fileStream = new NativeFileStream(path, NativeFileMode.Open, NativeFileAccess.Read);
        using var decoder = new SharpDX.WIC.BitmapDecoder(imageFactory, fileStream, SharpDX.WIC.DecodeOptions.CacheOnDemand);
        using var frame = decoder.GetFrame(0);
        using var frameConverter = new SharpDX.WIC.FormatConverter(imageFactory);
        frameConverter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppPBGRA);
        return new SharpDX.WIC.Bitmap(imageFactory, frameConverter, SharpDX.WIC.BitmapCreateCacheOption.CacheOnLoad);
    }
}