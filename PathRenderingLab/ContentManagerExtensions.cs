using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PathRenderingLab
{
    public static class ContentManagerExtensions
    {
        public static GraphicsDevice GetGraphicsDevice(this ContentManager content)
        {
            var service = content.ServiceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
            return service?.GraphicsDevice;
        }
    }
}
