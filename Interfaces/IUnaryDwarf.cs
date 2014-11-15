namespace Dwarf.Interfaces
{
    /// <summary>
    /// Dwarf specialization for tree structures
    /// </summary>
    public interface IUnaryDwarf : IDwarf
    {
        /// <summary>
        /// Gets the hierarchical search path of the IBussinessObject
        /// </summary>
        string GetPath();
    }
}
