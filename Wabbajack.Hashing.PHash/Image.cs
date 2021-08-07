using System.IO;
using System.Threading.Tasks;
using BCnEncoder.Decoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared.ImageFiles;
using Shipwreck.Phash;
using Shipwreck.Phash.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Wabbajack.DTOs.Texture;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;

namespace Wabbajack.Hashing.PHash
{
    public class ImageLoader
    {

        public static async ValueTask<ImageState> Load(AbsolutePath path)
        {
            await using var fs = path.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            return await Load(fs);
        }
        public static async ValueTask<ImageState> Load(Stream stream)
        {

            var decoder = new BcDecoder();
            var ddsFile = DdsFile.Load(stream);
            var data = await decoder.DecodeToImageRgba32Async(ddsFile);
            
            var state = new ImageState
            {
                Width = data.Width,
                Height = data.Height,
                Format = (DXGI_FORMAT)ddsFile.dx10Header.dxgiFormat
            };
            
            data.Mutate(x => x.Resize(512, 512, KnownResamplers.Welch).Grayscale(GrayscaleMode.Bt601));

            var hash = ImagePhash.ComputeDigest(new ImageBitmap(data));
            state.PerceptualHash = new DTOs.Texture.PHash(hash.Coefficients);
            return state;
        }
        
        public class ImageBitmap : IByteImage
        {
            private readonly Image<Rgba32> _image;

            public ImageBitmap(Image<Rgba32> image)
            {
                _image = image;
            }

            public int Width => _image.Width;
            public int Height => _image.Height;

            public byte this[int x, int y] => _image[x, y].R;
        }
    }
}