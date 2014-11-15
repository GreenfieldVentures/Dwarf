namespace Dwarf.DataAccess
{
    /// <summary>
    /// Different operators for combining queries
    /// </summary>
    public enum QueryMergers
    {
        /// <summary>
        /// Union
        /// </summary>
        Union,

        /// <summary>
        /// Intersect
        /// </summary>
        Intersect,

        /// <summary>
        /// Except
        /// </summary>
        Except
    }
}