﻿using System;

namespace Dwarf.Interfaces
{
    /// <summary>
    /// Base interface for all gems.
    /// </summary>
    public interface IGem : IComparable
    {
        /// <summary>
        /// Returns the Id of the instance
        /// </summary>
        object Id { get; }
    }
}