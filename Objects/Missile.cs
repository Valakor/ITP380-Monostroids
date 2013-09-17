using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace itp380.Objects
{
    public class Missile : GameObject
    {
        public Missile(Game game) :
            base(game)
        {
            m_ModelName = "Projectiles/Sphere";
            Scale = 0.05f;

            m_Timer.AddTimer("destroy self", 2.0f, RemoveSelf, false);
        }

        private void RemoveSelf() {
            GameState.Get().RemoveMissile(this);
        }
    }
}
