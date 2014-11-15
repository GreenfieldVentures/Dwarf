namespace Dwarf.Interfaces
{
    /// <summary>
    /// Represents a where condition to an sql query
    /// </summary>
    public interface IWhereCondition
    {
        /// <summary>
        /// Converts the current WhereCondition to an SQL Query
        /// </summary>
        string ToQuery();
    }
}