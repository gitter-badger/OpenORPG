﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Server.Infrastructure.World;

namespace Server.Infrastructure.Synchronization
{
    /// <summary>
    /// A collection of <see cref="SyncProperty"/>s and utility methods which can help with various tools they provide.
    /// </summary>
    public class SyncPropertyCollection
    {


        public SyncPropertyCollection()
        {


        }



        Dictionary<string, dynamic> _syncValues = new Dictionary<string, dynamic>();

        /// <summary>
        /// Writes a value to the internal list for syncing.
        /// </summary>
        public void WriteValue(string key, dynamic value)
        {
            if (_syncValues.ContainsKey(key))
                _syncValues[key] = value;
            else
                _syncValues.Add(key, value);
        }


        /// <summary>
        /// Gets and returns the modified properties on this collection.
        /// This should only be called when the values are going to be pushed out to the network.
        /// This means this should only be called once per tick.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, dynamic> GetAndFlushValues()
        {
            // Copy
            var values = _syncValues.ToDictionary(entry => entry.Key, entry => entry.Value);

            // Clear the list, thus letting the IsSynced be correct
            _syncValues.Clear();

            return values;
        }


        /// <summary>
        /// A property that allows the user to query whether it is in sync or not.
        /// This should change when a property is modified, but it has not been flushed yet.
        /// False will be returned if pending changes are available.
        /// 
        /// Otherwise, if no pending changes have been made it will be true.
        /// </summary>
        public bool IsSynced
        {
            get { return _syncValues.Count == 0;  }
        }


    }
}
