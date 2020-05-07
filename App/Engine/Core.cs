﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using App.Engine.Audio;
using App.Engine.Physics;
using App.Engine.Physics.Collision;
using App.Engine.Render;
using App.Model;
using App.Model.Entities;
using App.Model.LevelData;
using App.View;

namespace App.Engine
{
    public class Core
    {
        private readonly ViewForm viewForm;
        private readonly Stopwatch clock;
        private KeyStates keyState;
        private MouseState mouseState;
        private LevelManager levelManager;
        private Player player;
        private CustomCursor cursor;
        private Camera camera;

        private readonly ParticleFactory particleFactory;

        private Level currentLevel;

        private string updateTime;

        private void ResetControls()
        {
            keyState = new KeyStates();
            mouseState = new MouseState();
            //TODO reset cursor
            //TODO reset camera
        }

        private class KeyStates
        {
            public bool W, S, A, D, I;
            public int pressesOnIAmount;
        }

        private class MouseState
        {
            public bool LMB, RMB;
        }

        public Core(ViewForm viewForm, Size screenSize)
        {
            this.viewForm = viewForm;
            
            RenderMachine.Init(viewForm, screenSize);
            AudioEngine.Initialize();

            particleFactory = new ParticleFactory();
            
            SetLevels();
            player = currentLevel.Player;
            camera = new Camera(currentLevel.Player.Position, player.Radius, screenSize);
            SetCursor(currentLevel.Player.Position);
            keyState = new KeyStates();
            mouseState = new MouseState();
            clock = new Stopwatch();
            


            AudioEngine.PlayNewInstance(@"event:/themes/THEME");
        }

        private void SetLevels()
        {
            levelManager = new LevelManager();
            //currentLevel = levelManager.LoadLevel() ...;
        }
        


        private void SetCursor(Vector position)
        {
            var bmpCursor = levelManager.GetTileMap("crosshair.png");
            var cursorSprite = new Sprite
            (
                bmpCursor,
                3,
                0,
                9,
                new Size(64, 64));
            
            cursor = new CustomCursor(position.Copy(), cursorSprite);
            sprites.Add(cursor.SpriteContainer);
        }
        

        public void GameLoop(object sender, EventArgs args)
        {
            if (CollisionSolver.GetCollisionInfo(player.CollisionShape, currentLevel.Exit) != null)
            {
                Console.WriteLine("URA");
            }
            
            clock.Start();
            
            UpdateState();
            
            clock.Stop();
            var lTime = clock.ElapsedMilliseconds;
            clock.Reset();
            clock.Start();
            
            RenderPipeline.Render(currentLevel, camera.Size, camera.Position);
            
            if (keyState.pressesOnIAmount % 2 == 1)
                RenderPipeline.RenderDebugInfo(currentLevel, camera, cursor.Position, updateTime);
            
            AudioEngine.Update();
            
            clock.Stop();
            var rTime = clock.ElapsedMilliseconds;
            clock.Reset();
            updateTime = "logic=" + lTime + "ms render=" + rTime + "ms";
        }
        
        private void UpdateState()
        {
            const int step = 6;
            var previousPosition = player.Position.Copy();
            var playerVelocity = UpdatePlayerPosition(step);
            CorrectPlayer();
            
            currentLevel.CollisionsInfo = CollisionSolver.ResolveCollisions(currentLevel.SceneShapes);
            AudioEngine.UpdateListenerPosition(player.Position);
            var positionDelta = player.Position - previousPosition;
            cursor.MoveBy(viewForm.GetCursorDiff() + positionDelta);
            player.MeleeWeapon.MoveRangeShapeBy(positionDelta);
            UpdatePlayerByMouse();
            RotatePlayerLegs(playerVelocity);

            if (mouseState.RMB && player.MeleeWeapon.IsReady)
            {
                player.TakeMeleeWeapon();
                var wasHit = player.MeleeWeapon.Attack(currentLevel.Bots);
                if (wasHit) currentLevel.Particles.Add(particleFactory.CreateBigBloodSplash(player.Position + (cursor.Position - player.Position).Normalize() * player.Radius * 3));
            }
            else if (mouseState.LMB && player.CurrentWeapon.IsReady && player.MeleeWeapon.IsReady)
            {
                player.HideMeleeWeapon();
                var firedBullets = player.CurrentWeapon.Fire(player.Position, cursor);
                AudioEngine.PlayNewInstance("event:/gunfire/2D/misc/DROPPED_SHELL");
                currentLevel.Bullets.AddRange(firedBullets);
                currentLevel.Particles.Add(particleFactory.CreateShell(player.Position, cursor.Position - player.Position, player.CurrentWeapon));
            }

            UpdateCollectables();
            UpdateEntities();
            
            camera.UpdateCamera(player.Position, playerVelocity, cursor.Position, step);
            viewForm.CursorReset();

            foreach (var spriteContainer in currentLevel.Sprites)
                if (!spriteContainer.IsEmpty()) spriteContainer.Content.UpdateFrame();
            foreach (var unit in currentLevel.Particles)
                unit.UpdateFrame();
        }
        
        private Vector UpdatePlayerPosition(int step)
        {
            var delta = Vector.ZeroVector;
            if (keyState.W) 
                delta.Y -= step;
            if (keyState.S)
                delta.Y += step;
            if (keyState.A)
                delta.X -= step;
            if (keyState.D)
                delta.X += step;
            
            player.MoveBy(delta);
            return delta;
        }
        
        private void UpdatePlayerByMouse()
        {
            var direction = cursor.Position - player.Position;
            var dirAngle = direction.Angle;
            var angle = (float) (180 / Math.PI * dirAngle);
            player.TorsoContainer.Angle = angle;
            player.MeleeWeapon.RotateRangeShape(angle, player.Position);
        }
        
        private void RotatePlayerLegs(Vector delta)
        {
            if (delta.Equals(Vector.ZeroVector)) return;
            var dirAngle = Math.Atan2(-delta.Y, delta.X);
            var angle = 180 / Math.PI * dirAngle;
            player.LegsContainer.Angle = (float) angle;
        }
        
        private void CorrectPlayer()
        {
            var delta = Vector.ZeroVector;
            var rightBorder = currentLevel.LevelSizeInTiles.Width * tileSize;
            const int leftBorder = 0;
            var bottomBorder = currentLevel.LevelSizeInTiles.Height * tileSize;
            const int topBorder = 0;

            var a = player.Position.Y - player.Radius - topBorder;
            var b = player.Position.Y + player.Radius - bottomBorder;
            var c = player.Position.X - player.Radius - leftBorder;
            var d = player.Position.X + player.Radius - rightBorder;

            if (a < 0) delta.Y -= a;
            if (b > 0) delta.Y -= b;
            if (c < 0) delta.X -= c;
            if (d > 0) delta.X -= d;
            
            player.MoveBy(delta);
        }

        private void UpdateEntities()
        {
            UpdateBullets();
            UpdateTargets();

            player.IncrementWeaponsTick();
        }

        private void UpdateBullets()
        {
            foreach (var bullet in currentLevel.Bullets)
            {
                if (bullet.IsStuck) continue;
                if (bullet.StaticPenetrations.Count == 0)
                    CalculateStaticPenetrations(bullet);
                CalculateDynamicPenetrations(bullet);
                bullet.Update();
                if (bullet.ClosestPenetrationPoint != null)
                {
                    currentLevel.Particles.Add(particleFactory.CreateWallDust(bullet.ClosestPenetrationPoint, bullet.Velocity));
                    bullet.ClosestPenetrationPoint = null;
                }
            }
        }

        private void CalculateStaticPenetrations(Bullet bullet)
        {
            foreach (var obstacle in currentLevel.StaticShapes)
            {
                var penetrationTime = BulletCollisionDetector.AreCollideWithStatic(bullet, obstacle);
                if (penetrationTime == null) continue;
                var distanceToPenetrations = new float[2];
                for (var i = 0; i < 2; i++)
                    distanceToPenetrations[i] = bullet.Speed * penetrationTime[i];
                bullet.StaticPenetrations.Add(distanceToPenetrations);
            }
            bullet.CalculateTrajectory();
        }

        private void CalculateDynamicPenetrations(Bullet bullet)
        {
            foreach (var target in currentLevel.Bots)
            {
                var penetrationTimes = 
                    BulletCollisionDetector.AreCollideWithDynamic(bullet, target.CollisionShape, target.Velocity);
                if (penetrationTimes == null) continue;
                var penetrationPlace = bullet.Position + bullet.Velocity * penetrationTimes[0];
                target.TakeHit(bullet.Damage);
                if (target.Armour > 50) bullet.IsStuck = true;

                currentLevel.Particles.Add(particleFactory.CreateBloodSplash(penetrationPlace));
                currentLevel.Particles.Add(particleFactory.CreateBloodSplash(penetrationPlace));

                if (target.IsDead && !target.Velocity.Equals(Vector.ZeroVector))
                    target.MoveTo(target.CollisionShape.Center + target.Velocity * penetrationTimes[0]);
            }
        }

        /// <summary>
        /// Updates AI - it should be called every tick
        /// </summary>
        private void UpdateTargets()
        {
            foreach (var t in currentLevel.Bots)
            {
                t.Update();
            }
        }

        private void UpdateCollectables() // TODO
        {
            for (var i = 0; i < currentLevel.Collectables.Count; i++)
            {
                
            }
        }

        public void OnKeyDown(Keys keyPressed)
        {
            switch (keyPressed)
            {
                case Keys.W:
                    keyState.W = true;
                    break;

                case Keys.S:
                    keyState.S = true;
                    break;

                case Keys.A:
                    keyState.A = true;
                    break;

                case Keys.D:
                    keyState.D = true;
                    break;
                
                case Keys.I:
                    keyState.I = true;
                    keyState.pressesOnIAmount++;
                    break;
            }
        }

        public void OnKeyUp(Keys keyPressed)
        {
            switch (keyPressed)
            {
                case Keys.Escape:
                    AudioEngine.Release();
                    Application.Exit();
                    break;
                
                case Keys.W:
                    keyState.W = false;
                    break;

                case Keys.S:
                    keyState.S = false;
                    break;

                case Keys.A:
                    keyState.A = false;
                    break;

                case Keys.D:
                    keyState.D = false;
                    break;
                
                case Keys.I:
                    keyState.I = false;
                    break;
            }
        }
        
        public void OnMouseWheel(int wheelDelta)
        {
            if (wheelDelta > 0) player.MoveNextWeapon();
            else player.MovePreviousWeapon();
        }

        public void OnMouseDown(MouseButtons buttons)
        {
            switch (buttons)
            {
                case MouseButtons.Left:
                    mouseState.LMB = true;
                    break;
                
                case MouseButtons.Right:
                    mouseState.RMB = true;
                    break;
            }
        }

        public void OnMouseUp(MouseButtons buttons)
        {
            switch (buttons)
            {
                case MouseButtons.Left:
                    mouseState.LMB = false;
                    break;
                
                case MouseButtons.Right:
                    mouseState.RMB = false;
                    break;
            }
        }
    }
}