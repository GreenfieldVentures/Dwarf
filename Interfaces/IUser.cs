namespace Evergreen.Dwarf.Interfaces
{
    /// <summary>
    /// Interface for defining a system user
    /// </summary>
    public interface IUser
    {
        /// <summary>
        /// Gets or Sets the User Name (login name)
        /// </summary>
        string UserName { get; set; }
    }
}
