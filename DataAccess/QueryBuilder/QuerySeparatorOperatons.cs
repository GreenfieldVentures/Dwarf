namespace Evergreen.Dwarf.DataAccess
{
    /// <summary>
    /// Different operators available inside SelectOperator clauses
    /// </summary>
    public enum QuerySeparatorOperatons
    {
        /// <summary>
        /// None operation
        /// </summary>
        None,

        /// <summary>
        /// *
        /// </summary>
        Multiplication,

        /// <summary>
        /// /
        /// </summary>
        Division,

        /// <summary>
        /// -
        /// </summary>
        Subtraction,

        /// <summary>
        /// +
        /// </summary>
        Addition

    }
}