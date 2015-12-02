﻿// Bird.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Rabbit : Creature
    {

        public Rabbit()
        {

        }

        public Rabbit(string sprites, Vector3 position, ComponentManager manager, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, string name) :
            // Creature base constructor
            base
            (
                // Default stats
                new CreatureStats
                {
                    Dexterity = 6,
                    Constitution = 1,
                    Strength = 1,
                    Wisdom = 1,
                    Charisma = 1,
                    Intelligence = 1,
                    Size = 0.25f,
                    CanSleep = false
                },
                // Belongs to herbivore team
                "Herbivore",
                // Uses the default plan service
                PlayState.PlanService,
                // Belongs to the herbivore team
                manager.Factions.Factions["Herbivore"],
                // The physics component this creature belongs to
                new Physics
                (
                // It is called "bird"
                    "A Rabbit",
                // It's attached to the root component of the component manager
                    manager.RootComponent,
                // It is located at a position passed in as an argument
                    Matrix.CreateTranslation(position),
                // It has a size of 0.25 blocks
                    new Vector3(0.375f, 0.375f, 0.375f),
                // Its bounding box is located in its center
                    new Vector3(0.0f, 0.0f, 0.0f),
                //It has a mass of 1, a moment of intertia of 1, and very small friction/restitution
                    1.0f, 1.0f, 0.999f, 0.999f,
                // It has a gravity of 10 blocks per second downward
                    new Vector3(0, -10, 0)
                ),
                // All the rest of the arguments are passed in directly
                chunks, graphics, content, name
            )
        {
            // Called from constructor with appropriate sprite asset as a string
            Initialize(sprites);
        }

        /// <summary>
        /// Initialize function creates all the required components for the bird.
        /// </summary>
        /// <param name="spriteSheet">The sprite sheet to use for the bird</param>
        public void Initialize(string sprites)
        {
            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;

           
            // Create the sprite component for the bird.
            Sprite = new CharacterSprite
                                  (Graphics,
                                  Manager,
                                  "Rabbit Sprite",
                                  Physics,
                                  Matrix.CreateTranslation(0, 0.5f, 0)
                                  );

            CompositeAnimation.Descriptor descriptor =
                FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
                    ContentPaths.GetFileAsString(sprites));

            List<CompositeAnimation> animations = descriptor.GenerateAnimations("Rabbit");

            foreach (CompositeAnimation animation in animations)
            {
                Sprite.AddAnimation(animation);
            }

            // Used to grab other components
            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            // Used to sense hostile creatures
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            // Controls the behavior of the creature
            AI = new CreatureAI(this, "Rabbit AI", Sensors, PlanService);

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Bite", 0.01f, 2.0f, 1.0f, ContentPaths.Audio.bunny, ContentPaths.Effects.flash) };


            // The bird can hold one item at a time in its inventory
            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            };

            // The shadow is rotated 90 degrees along X, and is 0.25 blocks beneath the creature
            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.25f, 0.0f);
            shadowTransform *= Matrix.CreateScale(0.75f);

            SpriteSheet shadowTexture = new SpriteSheet(ContentPaths.Effects.shadowcircle);
            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, shadowTexture);

            // We set up the shadow's animation so that it's just a static black circle
            // TODO: Make the shadow set this up automatically
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");

            // The bird will emit a shower of blood when it dies
            DeathParticleTrigger = new ParticleTrigger("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            };

            // The bird is flammable, and can die when exposed to fire.
            Flames = new Flammable(Manager, "Flames", Physics, this);

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Rabbit");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the rabbit";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Rabbit",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Rabbit" } }
            };


            NoiseMaker.Noises.Add("Hurt", new List<string>() { ContentPaths.Audio.bunny });

        }
    }

    [JsonObject(IsReference = true)]
    public class Scorpion : Creature
    {

        public Scorpion()
        {

        }

        public Scorpion(string sprites, Vector3 position, ComponentManager manager, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, string name) :
            // Creature base constructor
            base
            (
                // Default stats
                new CreatureStats
                {
                    Dexterity = 6,
                    Constitution = 1,
                    Strength = 1,
                    Wisdom = 1,
                    Charisma = 1,
                    Intelligence = 1,
                    Size = 0.25f,
                    CanSleep = false
                },
                // Belongs to herbivore team
                "Carnivore",
                // Uses the default plan service
                PlayState.PlanService,
                // Belongs to the herbivore team
                manager.Factions.Factions["Carnivore"],
                // The physics component this creature belongs to
                new Physics
                (
                // It is called "bird"
                    "A Scorpion",
                // It's attached to the root component of the component manager
                    manager.RootComponent,
                // It is located at a position passed in as an argument
                    Matrix.CreateTranslation(position),
                // It has a size of 0.25 blocks
                    new Vector3(0.375f, 0.375f, 0.375f),
                // Its bounding box is located in its center
                    new Vector3(0.0f, 0.0f, 0.0f),
                //It has a mass of 1, a moment of intertia of 1, and very small friction/restitution
                    1.0f, 1.0f, 0.999f, 0.999f,
                // It has a gravity of 10 blocks per second downward
                    new Vector3(0, -10, 0)
                ),
                // All the rest of the arguments are passed in directly
                chunks, graphics, content, name
            )
        {
            // Called from constructor with appropriate sprite asset as a string
            Initialize(sprites);
        }

        /// <summary>
        /// Initialize function creates all the required components for the bird.
        /// </summary>
        /// <param name="spriteSheet">The sprite sheet to use for the bird</param>
        public void Initialize(string sprites)
        {
            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;


            // Create the sprite component for the bird.
            Sprite = new CharacterSprite
                                  (Graphics,
                                  Manager,
                                  "Scorpion Sprite",
                                  Physics,
                                  Matrix.CreateTranslation(0, 0.5f, 0)
                                  );

            CompositeAnimation.Descriptor descriptor =
                FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
                    ContentPaths.GetFileAsString(sprites));

            List<CompositeAnimation> animations = descriptor.GenerateAnimations("Scorpion");

            foreach (CompositeAnimation animation in animations)
            {
                Sprite.AddAnimation(animation);
            }

            // Used to grab other components
            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.2f, 0.2f, 0.2f), Vector3.Zero);

            // Used to sense hostile creatures
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            // Controls the behavior of the creature
            AI = new CreatureAI(this, "Scorpion AI", Sensors, PlanService);

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Sting", 0.01f, 2.0f, 1.0f, ContentPaths.Audio.hiss, ContentPaths.Effects.flash)};


            // The bird can hold one item at a time in its inventory
            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            };

            // The shadow is rotated 90 degrees along X, and is 0.25 blocks beneath the creature
            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.25f, 0.0f);
            shadowTransform *= Matrix.CreateScale(0.75f);

            SpriteSheet shadowTexture = new SpriteSheet(ContentPaths.Effects.shadowcircle);
            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, shadowTexture);

            // We set up the shadow's animation so that it's just a static black circle
            // TODO: Make the shadow set this up automatically
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");

            // The bird will emit a shower of blood when it dies
            DeathParticleTrigger = new ParticleTrigger("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            };

            // The bird is flammable, and can die when exposed to fire.
            Flames = new Flammable(Manager, "Flames", Physics, this);

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Scorpion");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the Scorpion";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Scorpion",
                Levels = new List<EmployeeClass.Level>() {new EmployeeClass.Level() {Index = 0, Name = "Scorpion"}}
            };

            NoiseMaker.Noises.Add("Hurt", new List<string>() { ContentPaths.Audio.hiss });
        }
    }


    [JsonObject(IsReference = true)]
    public class Frog : Creature
    {

        public Frog()
        {

        }

        public Frog(string sprites, Vector3 position, ComponentManager manager, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, string name) :
            // Creature base constructor
            base
            (
                // Default stats
                new CreatureStats
                {
                    Dexterity = 6,
                    Constitution = 1,
                    Strength = 1,
                    Wisdom = 1,
                    Charisma = 1,
                    Intelligence = 1,
                    Size = 0.25f,
                    CanSleep = false
                },
                // Belongs to herbivore team
                "Herbivore",
                // Uses the default plan service
                PlayState.PlanService,
                // Belongs to the herbivore team
                manager.Factions.Factions["Herbivore"],
                // The physics component this creature belongs to
                new Physics
                (
                // It is called "bird"
                    "A Frog",
                // It's attached to the root component of the component manager
                    manager.RootComponent,
                // It is located at a position passed in as an argument
                    Matrix.CreateTranslation(position),
                // It has a size of 0.25 blocks
                    new Vector3(0.375f, 0.375f, 0.375f),
                // Its bounding box is located in its center
                    new Vector3(0.0f, 0.0f, 0.0f),
                //It has a mass of 1, a moment of intertia of 1, and very small friction/restitution
                    1.0f, 1.0f, 0.999f, 0.999f,
                // It has a gravity of 10 blocks per second downward
                    new Vector3(0, -10, 0)
                ),
                // All the rest of the arguments are passed in directly
                chunks, graphics, content, name
            )
        {
            // Called from constructor with appropriate sprite asset as a string
            Initialize(sprites);
        }

        /// <summary>
        /// Initialize function creates all the required components for the bird.
        /// </summary>
        /// <param name="spriteSheet">The sprite sheet to use for the bird</param>
        public void Initialize(string sprites)
        {
            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;


            // Create the sprite component for the bird.
            Sprite = new CharacterSprite
                                  (Graphics,
                                  Manager,
                                  "Frog Sprite",
                                  Physics,
                                  Matrix.CreateTranslation(0, 0.5f, 0)
                                  );

            CompositeAnimation.Descriptor descriptor =
                FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
                    ContentPaths.GetFileAsString(sprites));

            List<CompositeAnimation> animations = descriptor.GenerateAnimations("Rabbit");

            foreach (CompositeAnimation animation in animations)
            {
                Sprite.AddAnimation(animation);
            }

            // Used to grab other components
            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.2f, 0.2f, 0.2f), Vector3.Zero);

            // Used to sense hostile creatures
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            // Controls the behavior of the creature
            AI = new CreatureAI(this, "Rabbit AI", Sensors, PlanService);

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Bite", 0.01f, 2.0f, 1.0f, ContentPaths.Audio.frog, ContentPaths.Effects.flash) };


            // The bird can hold one item at a time in its inventory
            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            };

            // The shadow is rotated 90 degrees along X, and is 0.25 blocks beneath the creature
            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.25f, 0.0f);
            shadowTransform *= Matrix.CreateScale(0.75f);

            SpriteSheet shadowTexture = new SpriteSheet(ContentPaths.Effects.shadowcircle);
            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, shadowTexture);

            // We set up the shadow's animation so that it's just a static black circle
            // TODO: Make the shadow set this up automatically
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");

            // The bird will emit a shower of blood when it dies
            DeathParticleTrigger = new ParticleTrigger("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            };

            // The bird is flammable, and can die when exposed to fire.
            Flames = new Flammable(Manager, "Flames", Physics, this);

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Frog");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the frog";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Frog",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Frog" } }
            };


            NoiseMaker.Noises.Add("Idle", new List<string>() { ContentPaths.Audio.frog });
            NoiseMaker.Noises.Add("Hurt", new List<string>() { ContentPaths.Audio.frog });
           
        }
    }

    [JsonObject(IsReference = true)]
    public class Bird : Creature
    {

        public Bird()
        {
            
        }

        public Bird(string sprites, Vector3 position, ComponentManager manager, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, string name) :
            // Creature base constructor
            base
            (
                // Default stats
                new CreatureStats
                {
                    Dexterity = 6,
                    Constitution = 1,
                    Strength = 1,
                    Wisdom = 1,
                    Charisma = 1,
                    Intelligence = 1,
                    Size = 0.25f,
                    CanSleep = false
                },
                // Belongs to herbivore team
                "Herbivore",
                // Uses the default plan service
                PlayState.PlanService,
                // Belongs to the herbivore team
                manager.Factions.Factions["Herbivore"],
                // The physics component this creature belongs to
                new Physics
                (
                    // It is called "bird"
                    "A Bird", 
                    // It's attached to the root component of the component manager
                    manager.RootComponent, 
                    // It is located at a position passed in as an argument
                    Matrix.CreateTranslation(position), 
                    // It has a size of 0.25 blocks
                    new Vector3(0.25f, 0.25f, 0.25f),
                    // Its bounding box is located in its center
                    new Vector3(0.0f, 0.0f, 0.0f), 
                    //It has a mass of 1, a moment of intertia of 1, and very small friction/restitution
                    1.0f, 1.0f, 0.999f, 0.999f, 
                    // It has a gravity of 10 blocks per second downward
                    new Vector3(0, -10, 0)
                ),
                // All the rest of the arguments are passed in directly
                chunks, graphics, content, name
            )
        {
            // Called from constructor with appropriate sprite asset as a string
            Initialize(new SpriteSheet(sprites));
        }

        /// <summary>
        /// Initialize function creates all the required components for the bird.
        /// </summary>
        /// <param name="spriteSheet">The sprite sheet to use for the bird</param>
        public void Initialize(SpriteSheet spriteSheet)
        {
            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;

            // The dimensions of each frame in the sprite sheet (in pixels), as given by the readme
            const int frameWidth = 24;
            const int frameHeight = 16;

            // Create the sprite component for the bird.
            Sprite = new CharacterSprite
                                  (Graphics, 
                                  Manager,
                                  "Bird Sprite", 
                                  Physics, 
                                  Matrix.CreateTranslation(0, 0.25f, 0)
                                  );

            // Flying animation (rows 4 5 6 and 7)
            Sprite.AddAnimation(CharacterMode.Flying, 
                                OrientedAnimation.Orientation.Forward, 
                                spriteSheet, 
                                // animation will play at 15 FPS
                                15.0f, 
                                frameWidth, frameHeight, 
                                // animation begins at row 4
                                4,
                                // It consists of columns 0, 1 and 2 looped forever
                                0, 1, 2);
            Sprite.AddAnimation(CharacterMode.Flying,
                                OrientedAnimation.Orientation.Left,
                                spriteSheet,
                                15.0f,
                                frameWidth, frameHeight,
                                5,
                                0, 1, 2);
            Sprite.AddAnimation(CharacterMode.Flying,
                                OrientedAnimation.Orientation.Right,
                                spriteSheet,
                                15.0f,
                                frameWidth, frameHeight,
                                6,
                                0, 1, 2);
            Sprite.AddAnimation(CharacterMode.Flying,
                                OrientedAnimation.Orientation.Backward,
                                spriteSheet,
                                15.0f,
                                frameWidth, frameHeight,
                                7,
                                0, 1, 2);

            // Hopping animation (rows 0 1 2 and 3)
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Forward, spriteSheet, 5.0f, frameWidth, frameHeight, 0, 0, 1);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Left, spriteSheet, 5.0f, frameWidth, frameHeight, 1, 0, 1);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Right, spriteSheet, 5.0f, frameWidth, frameHeight, 2, 0, 1);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Backward, spriteSheet, 5.0f, frameWidth, frameHeight, 3, 0, 1);

            // Idle animation (rows 0 1 2 and 3)
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Forward, spriteSheet, 5.0f, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Left, spriteSheet, 5.0f, frameWidth, frameHeight, 1, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Right, spriteSheet, 5.0f, frameWidth, frameHeight, 2, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Backward, spriteSheet, 5.0f, frameWidth, frameHeight, 3, 0);

            // Used to grab other components
            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.2f, 0.2f, 0.2f), Vector3.Zero);
            
            // Used to sense hostile creatures
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);
            
            // Controls the behavior of the creature
            AI = new BirdAI(this, "Bird AI", Sensors, PlanService);
            
            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack>{new Attack("Peck", 0.1f, 2.0f, 1.0f, ContentPaths.Audio.bird, ContentPaths.Effects.flash)};


            // The bird can hold one item at a time in its inventory
            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            };

            // The shadow is rotated 90 degrees along X, and is 0.25 blocks beneath the creature
            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.25f, 0.0f);
            shadowTransform *= Matrix.CreateScale(0.75f);

            SpriteSheet shadowTexture = new SpriteSheet(ContentPaths.Effects.shadowcircle);
            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, shadowTexture);

            // We set up the shadow's animation so that it's just a static black circle
            // TODO: Make the shadow set this up automatically
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");

            // The bird will emit a shower of blood when it dies
            DeathParticleTrigger = new ParticleTrigger("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            };

            // The bird is flammable, and can die when exposed to fire.
            Flames = new Flammable(Manager, "Flames", Physics, this);

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Bird");
            Physics.Tags.Add("Animal");

            NoiseMaker.Noises.Add("chirp", new List<string>(){ContentPaths.Audio.bird});

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the bird";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Bird",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Bird" } }
            };
        }
    }
}
