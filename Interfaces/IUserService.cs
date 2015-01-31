namespace Evergreen.Dwarf.Interfaces
{
    /// <summary>
    /// Interface for implementing the PersistanceFoundation's user service
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Gets or Sets the currently logged in  user
        /// </summary>
        IUser CurrentUser { get; set; }
    }
}
