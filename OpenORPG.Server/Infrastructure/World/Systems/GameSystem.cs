﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Infrastructure.World.Systems
{
    /// <summary>
    /// A game system is used to update the state of the world in a dynamic way.
    /// </summary>
    public abstract class GameSystem
    {

        private GameWorld _world;

        protected GameSystem(GameWorld world)
        {
            _world = world;
        }


        /// <summary>
        /// Updates the system and gives it a time slice to do any work it might need to do
        /// </summary>
        /// <param name="frameTime"></param>
        public abstract void Update(float frameTime);

        /// <summary>
        /// This method is automatically called when an entity is added to the system.
        /// </summary>
        /// <param name="entity">The entity that is being entered</param>
        public abstract void OnEntityAdded(Entity entity);

        /// <summary>
        /// This method is automatically called when an entity is being removed from the system.
        /// </summary>
        /// <param name="entity">The tnity being removed from the system</param>
        public abstract void OnEntityRemoved(Entity entity);

    }
}
