using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace PlatformerEngine.Core.Effects
{
    /// <summary>
    /// Single particle
    /// </summary>
    public class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Acceleration;
        public Color Color = Color.White;
        public float Rotation;
        public float RotationSpeed;
        public float Scale = 1.0f;
        public float ScaleSpeed;
        public float Alpha = 1.0f;
        public float AlphaDecay = 0.02f;
        public float Lifetime;
        public float Age;
        public bool IsAlive => Age < Lifetime && Alpha > 0;
    }

    /// <summary>
    /// Particle emitter
    /// </summary>
    public class ParticleEmitter
    {
        private List<Particle> particles = new List<Particle>();
        private Texture2D particleTexture;
        private Random random = new Random();

        public Vector2 Position { get; set; }
        public bool IsActive { get; set; } = true;
        public int MaxParticles { get; set; } = 100;
        public float EmissionRate { get; set; } = 10f;  // particles per second
        public float ParticleLifetime { get; set; } = 1.0f;
        public Vector2 ParticleVelocityMin { get; set; } = new Vector2(-50, -100);
        public Vector2 ParticleVelocityMax { get; set; } = new Vector2(50, 0);
        public Vector2 Gravity { get; set; } = new Vector2(0, 200);
        public Color ParticleColor { get; set; } = Color.White;
        public float ParticleScale { get; set; } = 1.0f;
        public float ParticleScaleVariation { get; set; } = 0.5f;

        private float emissionTimer;

        public ParticleEmitter(Texture2D texture, Vector2 position)
        {
            particleTexture = texture;
            Position = position;
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Emit particles
            emissionTimer += deltaTime;
            int particlesToEmit = (int)(emissionTimer * EmissionRate);
            emissionTimer -= particlesToEmit / EmissionRate;

            for (int i = 0; i < particlesToEmit && particles.Count < MaxParticles; i++)
            {
                EmitParticle();
            }

            // Update particles
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var particle = particles[i];
                particle.Age += deltaTime;

                if (!particle.IsAlive)
                {
                    particles.RemoveAt(i);
                    continue;
                }

                particle.Velocity += (particle.Acceleration + Gravity) * deltaTime;
                particle.Position += particle.Velocity * deltaTime;
                particle.Rotation += particle.RotationSpeed * deltaTime;
                particle.Scale += particle.ScaleSpeed * deltaTime;
                particle.Alpha -= particle.AlphaDecay * deltaTime;
            }
        }

        private void EmitParticle()
        {
            var particle = new Particle
            {
                Position = Position,
                Velocity = new Vector2(
                    Lerp(ParticleVelocityMin.X, ParticleVelocityMax.X, (float)random.NextDouble()),
                    Lerp(ParticleVelocityMin.Y, ParticleVelocityMax.Y, (float)random.NextDouble())
                ),
                Color = ParticleColor,
                Lifetime = ParticleLifetime * (0.8f + (float)random.NextDouble() * 0.4f),
                Scale = ParticleScale * (1.0f + ((float)random.NextDouble() - 0.5f) * ParticleScaleVariation),
                RotationSpeed = ((float)random.NextDouble() - 0.5f) * 4f
            };

            particles.Add(particle);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var particle in particles)
            {
                Color drawColor = particle.Color * particle.Alpha;
                spriteBatch.Draw(
                    particleTexture,
                    particle.Position,
                    null,
                    drawColor,
                    particle.Rotation,
                    new Vector2(particleTexture.Width / 2, particleTexture.Height / 2),
                    particle.Scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        public void Burst(int count)
        {
            for (int i = 0; i < count && particles.Count < MaxParticles; i++)
            {
                EmitParticle();
            }
        }

        public void Clear()
        {
            particles.Clear();
        }

        private float Lerp(float a, float b, float t) => a + (b - a) * t;
    }

    /// <summary>
    /// Manages multiple particle emitters
    /// </summary>
    public class ParticleManager
    {
        private List<ParticleEmitter> emitters = new List<ParticleEmitter>();
        private Texture2D defaultParticleTexture;

        public ParticleManager(GraphicsDevice graphicsDevice)
        {
            // Create default 4x4 white particle texture
            defaultParticleTexture = new Texture2D(graphicsDevice, 4, 4);
            Color[] data = new Color[16];
            for (int i = 0; i < 16; i++)
                data[i] = Color.White;
            defaultParticleTexture.SetData(data);
        }

        public void Update(GameTime gameTime)
        {
            for (int i = emitters.Count - 1; i >= 0; i--)
            {
                emitters[i].Update(gameTime);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var emitter in emitters)
            {
                emitter.Draw(spriteBatch);
            }
        }

        public ParticleEmitter CreateEmitter(Vector2 position, Texture2D texture = null)
        {
            var emitter = new ParticleEmitter(texture ?? defaultParticleTexture, position);
            emitters.Add(emitter);
            return emitter;
        }

        public void RemoveEmitter(ParticleEmitter emitter)
        {
            emitters.Remove(emitter);
        }

        /// <summary>
        /// Create explosion effect
        /// </summary>
        public void CreateExplosion(Vector2 position, Color color, int particleCount = 20)
        {
            var emitter = CreateEmitter(position);
            emitter.ParticleColor = color;
            emitter.ParticleVelocityMin = new Vector2(-150, -150);
            emitter.ParticleVelocityMax = new Vector2(150, 150);
            emitter.ParticleLifetime = 0.5f;
            emitter.Gravity = new Vector2(0, 300);
            emitter.Burst(particleCount);
            emitter.IsActive = false;
        }

        /// <summary>
        /// Create dust effect (for landing, running, etc.)
        /// </summary>
        public void CreateDust(Vector2 position, Color color, int particleCount = 5)
        {
            var emitter = CreateEmitter(position);
            emitter.ParticleColor = color;
            emitter.ParticleVelocityMin = new Vector2(-30, -50);
            emitter.ParticleVelocityMax = new Vector2(30, -10);
            emitter.ParticleLifetime = 0.3f;
            emitter.Gravity = new Vector2(0, 100);
            emitter.Burst(particleCount);
            emitter.IsActive = false;
        }
    }
}
