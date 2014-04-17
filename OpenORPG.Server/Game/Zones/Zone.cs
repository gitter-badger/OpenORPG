﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.Game.Entities;
using Server.Game.Network.Packets;
using Server.Game.Network.Packets.Server;
using Server.Game.Zones.Spawns;
using Server.Infrastructure.Logging;
using Server.Infrastructure.Math;
using Server.Infrastructure.Network.Packets;
using Server.Infrastructure.World;
using Server.Infrastructure.World.Systems;
using Server.Utils;
using TiledSharp;

namespace Server.Game.Zones
{
    public class GameClientCollection : IEnumerable<GameClient>
    {
        private readonly List<GameClient> _internalList;
        private readonly Zone _zone;

        private const int ViewWidth = 1920;
        private const int ViewHeight = 1080;

        public GameClientCollection(Zone zone)
        {
            _zone = zone;

            _internalList = new List<GameClient>();
        }

        IEnumerator<GameClient> IEnumerable<GameClient>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }


        internal void Add(GameClient client)
        {
            client.Zone = _zone;
            _internalList.Add(client);
        }

        internal void Remove(GameClient client)
        {
            _internalList.Remove(client);
            client.Zone = null;
        }
    }

    /// <summary>
    ///     A zone represents a single given subspace in the world that can be contained.
    ///     This is similar to a 'map' or 'area' in traditional MMO worlds.
    ///     The world within the zone contains the actual physical mainfestation and collection of objects.
    /// </summary>
    public class Zone
    {

        private Rectangle topZoneArea, bottomZoneArea, leftZoneArea, rightZoneArea;


        public Zone(long id)
        {
            GameClients = new GameClientCollection(this);
            ChatChannel = ChatManager.Current.CreateChannel(ChannelType.Zone);
            Id = id;


            string zonePath = Path.Combine(PathHelper.AssetBasePath, PathHelper.MapPath,
                                           id + PathHelper.MapExtension);
            try
            {
                TileMap = new TmxMap(zonePath);
            }
            catch (FileNotFoundException exception)
            {
                // Mark the world as offline
                Available = false;

                Logger.Instance.Error("The world in zone #{0} could not be started. The tilemap could not be found.", id);
            }

            AddGameSystems();

            // Create our empty slots
            ZoneExitPoints = new long[4] { -1, -1, -1, -1 };

            // We can extract and setup our exit points now
            SetupExitPoints();

            // Setup our sensors
            SetupExitRectangles();
        }

        private void SetupExitRectangles()
        {
            // Create a top sensor
            topZoneArea = new Rectangle(0, 0, TileMap.Width * TileMap.TileWidth, TileMap.TileHeight);

            // Bottom sensor
            bottomZoneArea = new Rectangle(0, TileMap.Height * TileMap.TileHeight - TileMap.Height, TileMap.Width * TileMap.TileWidth, TileMap.TileHeight);

            // Left
            leftZoneArea = new Rectangle(0, 0, TileMap.TileWidth, TileMap.Height * TileMap.TileHeight);

            // Right
            rightZoneArea = new Rectangle(0, TileMap.Width * (TileMap.TileWidth - 1), TileMap.TileWidth, TileMap.Height * TileMap.TileHeight);

        }

        private void SetupExitPoints()
        {
            string up;
            string right;
            string down;
            string left;

            var suc = TileMap.Properties.TryGetValue("UpExitPoint", out up);
            if (suc)
                ZoneExitPoints[(int)Direction.North] = Convert.ToInt64(up);

            suc = TileMap.Properties.TryGetValue("RightExitPoint", out right);
            if (suc)
                ZoneExitPoints[(int)Direction.East] = Convert.ToInt64(right);

            suc = TileMap.Properties.TryGetValue("DownExitPoint", out  down);
            if (suc)
                ZoneExitPoints[(int)Direction.South] = Convert.ToInt64(down);

            suc = TileMap.Properties.TryGetValue("LeftExitPoint", out left);
            if (suc)
                ZoneExitPoints[(int)Direction.West] = Convert.ToInt64(left);

        }

        public bool CanLeave(Direction direction, Player player)
        {

            //TODO: Please don't hard-code me
            var playerRect = new Rectangle(player.X, player.Y, 32, 32);

            switch (direction)
            {
                case Direction.North:
                    return topZoneArea.Intersects(playerRect);
                case Direction.East:
                    return rightZoneArea.Intersects(playerRect);
                case Direction.South:
                    return bottomZoneArea.Intersects(playerRect);
                case Direction.West:
                    return leftZoneArea.Intersects(playerRect);
            }

            return false;
        }

        private void AddGameSystems()
        {
            GameSystems.Add(new SpawnGameSystem(this));
        }

        /// <summary>
        /// A collection of entities that this game world is responsible for handling.
        /// </summary>
        protected List<Entity> Entities = new List<Entity>();

        private readonly List<Entity> _toAdd = new List<Entity>();
        private readonly List<Entity> _toRemove = new List<Entity>();

        protected List<GameSystem> GameSystems = new List<GameSystem>();

        /// <summary>
        /// An array of zone exit point IDs that users might choose to leave by
        /// </summary>
        public long[] ZoneExitPoints { get; set; }

        /// <summary>
        /// Indicates whether or not the world is currently available. If something fatal happens which requires
        /// recovery, this flag will be set.
        /// 
        /// Once this flag is set, only a manual reset of the world can fix it.
        /// </summary>
        public bool Available { get; private set; }

        /// <summary>
        /// This is the internal representation of the world.
        /// </summary>
        public TmxMap TileMap { get; set; }

        public IEnumerable<Character> ZoneCharacters
        {
            get { return (IEnumerable<Character>)Entities.Where(x => x is Character); }
        }


        /// <summary>
        /// Retrieves a particular game system from the world for usage.
        /// </summary>
        /// <typeparam name="T">The type of game system to retrieve from the collection</typeparam>
        /// <returns></returns>
        public T GetGameSystem<T>() where T : GameSystem
        {
            return (T)GameSystems.First(sys => sys.GetType() == typeof(T));
        }

        public void AddEntity(Entity entity)
        {
            // Assign the world
            entity.Zone = this;
            _toAdd.Add(entity);
            NotifySystemsAdd(entity);
        }

        private void NotifySystemsAdd(Entity entity)
        {
            foreach (GameSystem system in GameSystems)
                system.OnEntityAdded(entity);
        }

        private void NotifySystemsRemove(Entity entity)
        {
            foreach (GameSystem system in GameSystems)
                system.OnEntityRemoved(entity);
        }

        /// <summary>
        /// This is invoked when a new player is added so the according steps can be taken.
        /// </summary>
        /// <param name="player"></param>
        private void ProcessNewPlayer(Player player)
        {
            // Notify the player about this change
            var packet = new ServerZoneChangedPacket(Id, player.Id, Entities);
            player.Client.Send(packet);
        }

        public void RemoveEntity(Entity entity)
        {
            entity.Zone = null;
            _toRemove.Add(entity);
            NotifySystemsRemove(entity);
        }


        public void Update(TimeSpan deltaTime)
        {
            // Remove and add elements that need to be

            foreach (var entity in _toRemove)
            {
                Entities.Remove(entity);
                ProcessRemovedEntity(entity);
            }

            foreach (var entity in _toAdd)
            {
                Entities.Add(entity);

                if (entity is Player)
                    ProcessNewPlayer(entity as Player);
                ProcessAddedEntity(entity);
            }


            _toRemove.Clear();
            _toAdd.Clear();

            SyncEntityProperties();
        }

        private void ProcessAddedEntity(Entity entity)
        {
            var packet = new ServerMobCreatePacket(entity);
            SendToEveryone(packet);
        }

        private void ProcessRemovedEntity(Entity entity)
        {
            var packet = new ServerMobDestroyPacket(entity.Id);
            SendToEveryone(packet);
        }

        private void SyncEntityProperties()
        {
            // Check for syncing of packets
            foreach (var entity in Entities)
            {
                var properties = entity.GetSyncProperties();

                if (properties != null)
                {
                    var packet = new ServerEntityPropertyChange(properties, entity.Id);
                    SendToEveryone(packet);
                }
            }
        }

        /// <summary>
        /// Sends a packet to the entire zone.
        /// </summary>
        /// <param name="packet">The packet to send to the zone</param>
        public void SendToEveryone(IPacket packet)
        {
            foreach (var client in GameClients)
            {
                client.Send(packet);
            }
        }

        /// <summary>
        /// Sends a packet to all clients in range of the source. 
        /// This is useful for information that only needs to be broadcast to some.
        /// </summary>
        /// <param name="packet">The packet to send to the clients</param>
        /// <param name="source">The source entity we check all other clients against</param>
        public void SendToEntitiesInRange(IPacket packet, Entity source)
        {
            foreach (var client in GameClients.Where(client => client.HeroEntity.IsInView(source)))
            {
                client.Send(packet);
            }
        }



        /// <summary>
        /// Sends a packet to all clients in range of the source, excluding the source.
        /// </summary>
        /// <param name="packet">The packet to be sent to the clients.</param>
        /// <param name="source">The source entity that is checked against for ranges.</param>
        public void SendToEntitiesInRangeExcludingSource(IPacket packet, Entity source)
        {
            foreach (var client in GameClients.Where(client => client.HeroEntity.IsInView(source) && (client.HeroEntity.Id != source.Id)))
            {
                client.Send(packet);
            }

        }

        /// <summary>
        ///     The name of the actual zone
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     the unique ID that belongs to this particular zone
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        ///     A read-only copy of the world, it cannot be over-written.
        /// </summary>
        public GameWorld World { get; private set; }

        /// <summary>
        ///     A collection of <see cref="GameClient" />s that are a part of this zone.
        /// </summary>
        public GameClientCollection GameClients { get; private set; }


        /// <summary>
        /// A reference to the chat channel this zone maintains, used for communication purposes
        /// </summary>
        public ChatChannel ChatChannel { get; set; }


        public void OnClientLeave(GameClient client)
        {
            GameClients.Remove(client);

            ChatManager.Current.Global.Leave(client);
            ChatChannel.Leave(client);

            RemoveEntity(client.HeroEntity);
        }

        public void OnClientEnter(GameClient client, Player heroEntity)
        {
            GameClients.Add(client);
            ChatManager.Current.Global.Join(client);
            ChatChannel.Join(client);
            AddEntity(heroEntity);

            string name = heroEntity.Name;
            Logger.Instance.Info("{0} has entered {1}", name, Name);

        }



    }
}