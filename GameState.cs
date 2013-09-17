//-----------------------------------------------------------------------------
// The main GameState Singleton. All actions that change the game state,
// as well as any global updates that happen during gameplay occur in here.
// Because of this, the file is relatively lengthy.
//
// __Defense Sample for Game Programming Algorithms and Techniques
// Copyright (C) Sanjay Madhav. All rights reserved.
//
// Released under the Microsoft Permissive License.
// See LICENSE.txt for full details.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace itp380
{
	public enum eGameState
	{
		None = 0,
		MainMenu,
		Gameplay,
	}

	public class GameState : itp380.Patterns.Singleton<GameState>
	{
		Game m_Game;
        Objects.Ship m_Ship;
        List<Objects.Asteroid> m_Asteroids = new List<Objects.Asteroid>();
        List<Objects.Missile> m_Missiles = new List<Objects.Missile>();
        Random m_Random = new Random();
        bool canFire = true;
        bool vulnerable = false;
        int waveNum = 0;
        
        private int m_Score = 0;
        public int Score
        {
            get { return m_Score; }
        }

        private int m_Lives = 3;
        public int Lives
        {
            get { return m_Lives; }
        }

		eGameState m_State;
		public eGameState State
		{
			get { return m_State; }
		}

		eGameState m_NextState;
		Stack<UI.UIScreen> m_UIStack;
		bool m_bPaused = false;
		public bool IsPaused
		{
			get { return m_bPaused; }
			set	{ m_bPaused = value; }
		}

		// Keeps track of all active game objects
		LinkedList<GameObject> m_GameObjects = new LinkedList<GameObject>();

		// Camera Information
		Camera m_Camera;
		public Camera Camera
		{
			get { return m_Camera; }
		}

		public Matrix CameraMatrix
		{
			get { return m_Camera.CameraMatrix; }
		}

		// Timer class for the global GameState
		Utils.Timer m_Timer = new Utils.Timer();

		UI.UIGameplay m_UIGameplay;
		
		public void Start(Game game)
		{
			m_Game = game;
			m_State = eGameState.None;
			m_UIStack = new Stack<UI.UIScreen>();

			m_Camera = new Camera(m_Game);
		}

		public void SetState(eGameState NewState)
		{
			m_NextState = NewState;
		}

		private void HandleStateChange()
		{
			if (m_NextState == m_State)
				return;

			switch (m_NextState)
			{
				case eGameState.MainMenu:
					m_UIStack.Clear();
					m_UIGameplay = null;
					m_Timer.RemoveAll();
					m_UIStack.Push(new UI.UIMainMenu(m_Game.Content));
					ClearGameObjects();
					break;
				case eGameState.Gameplay:
					SetupGameplay();
					break;
			}

			m_State = m_NextState;
		}

		protected void ClearGameObjects()
		{
			// Clear out any and all game objects
			foreach (GameObject o in m_GameObjects)
			{
				RemoveGameObject(o, false);
			}
			m_GameObjects.Clear();
		}

		public void SetupGameplay()
		{
			ClearGameObjects();
			m_UIStack.Clear();
			m_UIGameplay = new UI.UIGameplay(m_Game.Content);
			m_UIStack.Push(m_UIGameplay);

			m_bPaused = false;
			GraphicsManager.Get().ResetProjection();
			
			m_Timer.RemoveAll();

            m_Ship = new Objects.Ship(m_Game);
            SpawnGameObject(m_Ship);

            m_Missiles.Clear();
            m_Asteroids.Clear();

            canFire = true;
            m_Lives = 3;
            lifeBonus = 0;
            m_Score = 0;
            waveNum = 0;
            SpawnWave();
            Respawn();
			// TODO: Add any gameplay setup here
		}

        public void Update(float fDeltaTime)
		{
			HandleStateChange();

			switch (m_State)
			{
				case eGameState.MainMenu:
					UpdateMainMenu(fDeltaTime);
					break;
				case eGameState.Gameplay:
					UpdateGameplay(fDeltaTime);
					break;
			}

			foreach (UI.UIScreen u in m_UIStack)
			{
				u.Update(fDeltaTime);
			}
		}

		void UpdateMainMenu(float fDeltaTime)
		{

		}

		void UpdateGameplay(float fDeltaTime)
		{
			if (!IsPaused)
			{
				m_Camera.Update(fDeltaTime);

				// Update objects in the world
				// We have to make a temp copy in case the objects list changes
				LinkedList<GameObject> temp = new LinkedList<GameObject>(m_GameObjects);
				foreach (GameObject o in temp)
				{
					if (o.Enabled)
					{
						o.Update(fDeltaTime);
					}
				}
				m_Timer.Update(fDeltaTime);

				// TODO: Any update code not for a specific game object should go here
                for (int i = 0; i < m_Missiles.Count; i++)
                {
                    for (int j = 0; j < m_Asteroids.Count; j++)
                    {
                        if (m_Missiles[i].WorldBounds.Intersects(m_Asteroids[j].WorldBounds))
                        {
                            if (m_Asteroids[j].Size == Objects.asteroidSize.Large)
                            {
                                SpawnSmallAsteroid(m_Asteroids[j].Position);
                                SpawnSmallAsteroid(m_Asteroids[j].Position);
                                UpdateScore(100);
                            }
                            else
                            {
                                UpdateScore(50);
                            }
                             
                            RemoveMissile(m_Missiles[i]);
                            RemoveAsteroid(m_Asteroids[j]);
                            SoundManager.Get().PlaySoundCue("Snared");

                            i--;
                            break;
                        }
                    }
                }

                for (int i = 0; i < m_Asteroids.Count; i++)
                {
                    if (m_Asteroids[i].WorldBounds.Intersects(m_Ship.WorldBounds) && m_Ship.Enabled && vulnerable) {
                        RemoveLife();
                    }
                }

                if (m_Asteroids.Count == 0)
                {
                    SpawnWave();
                }
			}
		}

        private int lifeBonus = 0;
        public void UpdateScore(int by)
        {
            m_Score += by;
            lifeBonus += by;

            if (lifeBonus >= 4000)
            {
                lifeBonus -= 4000;
                m_Lives++;
                SoundManager.Get().PlaySoundCue("Victory");
            }
        }
        
        public void SpawnGameObject(GameObject o)
		{
			o.Load();
			m_GameObjects.AddLast(o);
			GraphicsManager.Get().AddGameObject(o);
		}

        public void SpawnWave()
        {          
            for (int i = 0; i < 10 + waveNum; i++)
            {
                SpawnAsteroid();
            }
            waveNum++;

            vulnerable = false;
            m_Timer.AddTimer("invulnerable", 2.0f, SetVulnerable, false);
        }

        public static float BIGASTEROIDVELOCITY = 1.5f;
        public void SpawnAsteroid()
        {
            Objects.Asteroid tempAsteroid = new Objects.Asteroid(m_Game);
            m_Asteroids.Add(tempAsteroid);

            float x = m_Random.Next(-100, 100) / 10.0f;
            float y = m_Random.Next(-75, 75) / 10.0f;
            Vector3 tempVector = new Vector3(x, y, 0);
            tempAsteroid.Position = tempVector;

            float rotation = (float)m_Random.NextDouble() * MathHelper.TwoPi;
            tempAsteroid.Angle = rotation;

            float xDir = m_Random.Next(-100, 100) / 10.0f;
            float yDir = m_Random.Next(-75, 75) / 10.0f;
            Vector3 tempVectorDir = new Vector3(xDir, yDir, 0);
            tempVectorDir.Normalize();
            tempVectorDir *= BIGASTEROIDVELOCITY;
            tempAsteroid.Velocity = tempVectorDir;
            tempAsteroid.scaleVelocity(1 + (waveNum-1)/15.0f);

            SpawnGameObject(tempAsteroid);
        }

        public static float SMALLASTEROIDVELOCITY = 2.5f;
        public void SpawnSmallAsteroid(Vector3 position)
        {
            Objects.Asteroid tempAsteroid = new Objects.Asteroid(m_Game);
            m_Asteroids.Add(tempAsteroid);

            tempAsteroid.Position = position;

            float rotation = (float)m_Random.NextDouble() * MathHelper.TwoPi;
            tempAsteroid.Angle = rotation;

            float xDir = m_Random.Next(-100, 100) / 10.0f;
            float yDir = m_Random.Next(-75, 75) / 10.0f;
            Vector3 tempVectorDir = new Vector3(xDir, yDir, 0);
            tempVectorDir.Normalize();
            tempVectorDir *= SMALLASTEROIDVELOCITY;
            tempAsteroid.Velocity = tempVectorDir;
            tempAsteroid.scaleVelocity(1 + (waveNum-1)/15.0f);

            tempAsteroid.Scale *= 0.5f;
            tempAsteroid.Size = Objects.asteroidSize.Small;

            SpawnGameObject(tempAsteroid);
        }

        private static float MISSILEVELOCITY = 9.0f;
        public void SpawnMissile()
        {
            Objects.Missile tempMissile = new Objects.Missile(m_Game);
            m_Missiles.Add(tempMissile);

            tempMissile.Position = m_Ship.Position + m_Ship.Forward * 0.40f;
            tempMissile.Velocity = m_Ship.Forward * MISSILEVELOCITY;


            SpawnGameObject(tempMissile);
        }

        public void ResetFire()
        {
            canFire = true;
        }

        public void RemoveMissile(Objects.Missile missile)
        {
            m_Missiles.Remove(missile);
            RemoveGameObject(missile);
        }

        public void RemoveAsteroid(Objects.Asteroid asteroid)
        {
            m_Asteroids.Remove(asteroid);
            RemoveGameObject(asteroid);
        }

        public void RemoveLife()
        {
            SoundManager.Get().PlaySoundCue("Error");
            m_Ship.Enabled = false;
            m_Lives--;
            if (m_Lives <= 0) {
                GameOver(false);
                return;
            }
            m_Timer.AddTimer("respawn", 3.0f, Respawn, false);
        }

        public void Respawn()
        {
            m_Ship.Position = Vector3.Zero;
            m_Ship.Velocity = Vector3.Zero;
            m_Ship.Angle = 0f;

            vulnerable = false;
            m_Timer.AddTimer("invulnerable", 2.0f, SetVulnerable, false);
            m_Timer.AddTimer("flashShip", 0.20f, FlashShip, true);
            m_Ship.Enabled = true;
        }

        public void SetVulnerable()
        {
            m_Timer.RemoveTimer("flashShip");
            vulnerable = true;
            m_Ship.shouldDraw = true;
        }

        public void FlashShip()
        {
            m_Ship.shouldDraw = !m_Ship.shouldDraw;
        }

		public void RemoveGameObject(GameObject o, bool bRemoveFromList = true)
		{
			o.Enabled = false;
			o.Unload();
			GraphicsManager.Get().RemoveGameObject(o);
			if (bRemoveFromList)
			{
				m_GameObjects.Remove(o);
			}
		}

		public void MouseClick(Point Position)
		{
			if (m_State == eGameState.Gameplay && !IsPaused)
			{
				// TODO: Respond to mouse clicks here
			}
		}

		// I'm the last person to get keyboard input, so don't need to remove
        private static float POSACCEL = 4.00f;
        private static float MAXSPEED = 7.0f;
		public void KeyboardInput(SortedList<eBindings, BindInfo> binds, float fDeltaTime)
		{
			if (m_State == eGameState.Gameplay && !IsPaused)
			{
                Vector3 shipDirection = m_Ship.Forward;
                Vector3 velocityDirection = m_Ship.Velocity;

                if (binds.ContainsKey(eBindings.Ship_Left))
                {
                    m_Ship.Angle += 3.5f * fDeltaTime;
                }
                if (binds.ContainsKey(eBindings.Ship_Right))
                {
                    m_Ship.Angle -= 3.5f * fDeltaTime;
                }
                if (binds.ContainsKey(eBindings.Ship_Forward))
                {
                    if (Vector3.Dot(velocityDirection, m_Ship.Forward) <= MAXSPEED)
                    {
                        m_Ship.Velocity += shipDirection * fDeltaTime * POSACCEL;
                    }
                }
                if (binds.ContainsKey(eBindings.Ship_Back))
                {
                    if (Vector3.Dot(velocityDirection, m_Ship.Forward) <= MAXSPEED)
                    {
                        m_Ship.Velocity -= shipDirection * fDeltaTime * POSACCEL;
                    }
                }             
                if (binds.ContainsKey(eBindings.Ship_Fire))
                {
                    if (canFire && m_Ship.Enabled)
                    {
                        SpawnMissile();
                        SoundManager.Get().PlaySoundCue("Shoot");
                    }
                    canFire = false;
                    m_Timer.AddTimer("allow firing", 0.5f, ResetFire, false);
                }
                // TODO: Add keyboard input handling for Gameplay
			}
		}

		public UI.UIScreen GetCurrentUI()
		{
			return m_UIStack.Peek();
		}

		public int UICount
		{
			get { return m_UIStack.Count; }
		}

		// Has to be here because only this can access stack!
		public void DrawUI(float fDeltaTime, SpriteBatch batch)
		{
			// We draw in reverse so the items at the TOP of the stack are drawn after those on the bottom
			foreach (UI.UIScreen u in m_UIStack.Reverse())
			{
				u.Draw(fDeltaTime, batch);
			}
		}

		// Pops the current UI
		public void PopUI()
		{
			m_UIStack.Peek().OnExit();
			m_UIStack.Pop();
		}

		public void ShowPauseMenu()
		{
			IsPaused = true;
			m_UIStack.Push(new UI.UIPauseMenu(m_Game.Content));
		}

		public void Exit()
		{
			m_Game.Exit();
		}

		void GameOver(bool victorious)
		{
			IsPaused = true;
			m_UIStack.Push(new UI.UIGameOver(m_Game.Content, victorious));
		}
	}
}
