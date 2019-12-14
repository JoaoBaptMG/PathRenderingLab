using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathRenderingLab.PaintServers
{
    /// <summary>
    /// Represents a paint server on the client side, with attributes and methods
    /// to help populate an effect with the correct parameters
    /// </summary>
    public interface IPaintServer
    {
        /// <summary>
        /// The name of the effect that must be loaded for the paint server to be properly rendered
        /// </summary>
        string EffectName { get; }

        /// <summary>
        /// Prepare the resources needed for that paint server
        /// </summary>
        /// <param name="content">A content manager for the project</param>
        void PrepareOutsideResources(ContentManager content);

        /// <summary>
        /// Sets the parameters of the effect to the correct parameters on the paint server
        /// </summary>
        /// <param name="effect">The effect to load the parameters to</param>
        void SetEffectParameters(Effect effect);
    }
}
