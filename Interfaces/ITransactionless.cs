using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarf.Interfaces
{
    /// <summary>
    /// Interface to to declare type as not a subject of transactions
    /// </summary>
    public interface ITransactionless : ICacheless
    {
    }
}
