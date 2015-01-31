using System;
using Evergreen.Dwarf.Interfaces;

namespace Evergreen.Dwarf.Attributes
{
    /// <summary>
    /// Lets other mechanisms know not to invoke properties with this attribute during validation
    /// </summary>
    public class UnvalidatableAttribute : Attribute, IUnvalidatable
    {
        
    }
}