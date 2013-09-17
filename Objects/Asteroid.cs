using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace itp380.Objects
{
    public enum asteroidSize
    {
        Small,
        Large
    }
    
    public class Asteroid : GameObject
    {
        private asteroidSize m_size = asteroidSize.Large;
        public asteroidSize Size
        {
            get { return m_size; }
            set { m_size = value; }
        }
        
        public Asteroid(Game game) :
            base(game)
        {
            m_ModelName = "Asteroid";
        }

        public void scaleVelocity (float scale) {
            Velocity *= scale;
        }
    }
}
