namespace Wabbajack.DTOs.Texture
{
    public class ImageState
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public DXGI_FORMAT Format { get; set; }
        public PHash PerceptualHash { get; set; }
    }
}