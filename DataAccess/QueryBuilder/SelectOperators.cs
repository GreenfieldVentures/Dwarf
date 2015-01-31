namespace Evergreen.Dwarf.DataAccess
{
    /// <summary>
    /// Different operators available for select clauses
    /// </summary>
    public enum SelectOperators
    {
        /// <summary>
        /// SUM()
        /// </summary>
        Sum,

        /// <summary>
        /// COUNT()
        /// </summary>
        Count,

        /// <summary>
        /// AVG()
        /// </summary>
        Avg,

        /// <summary>
        /// MIN()
        /// </summary>
        Min,

        /// <summary>
        /// MAX()
        /// </summary>
        Max,
    }
}