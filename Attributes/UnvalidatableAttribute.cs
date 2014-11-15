using System;
using Dwarf.Interfaces;

namespace Dwarf.Attributes
{
    /// <summary>
    /// Lets other mechanisms know not to invoke properties with this attribute durin validation
    /// </summary>
    public class UnvalidatableAttribute : Attribute, IUnvalidatable
    {
        
    }
}