using Evergreen.Dwarf.DataAccess;

namespace Evergreen.Dwarf.Interfaces
{
    internal interface IQueryConstructor
    {
        string Top(int i);
        string Limit(int offset, int rows);
        string Offset(int offset);
        string Distinct { get; }
        string LeftContainer { get; }
        string RightContainer { get; }
        string TableNamePrefix { get; }
        string InnerJoin { get; }
        string LeftOuterJoin { get; }
        string Date(string columnName);
        string DatePart(DateParts datePart, string targetColumn);
    }
}