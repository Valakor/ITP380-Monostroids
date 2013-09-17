using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace itp380.Objects
{
    public class Ship : GameObject
    {
        public Ship(Game game) :
            base(game)
        {
            m_ModelName = "Miner/Miner";
        }
    }
}
