using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using Evergreen.Dwarf.DataAccess;
using Evergreen.Dwarf.Interfaces;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf
{
    /// <summary>
    /// Internal static object containing all wired up lists to make the framework more responsive...
    /// </summary>
    internal static class Cfg
    {
        /// <summary>
        /// All primary key properties per type
        /// </summary>
        internal static Dictionary<Type, List<ExpressionProperty>> PKProperties = new Dictionary<Type, List<ExpressionProperty>>();
        
        /// <summary>
        /// All DB properties per type
        /// </summary>
        internal static Dictionary<Type, List<ExpressionProperty>> DBProperties = new Dictionary<Type, List<ExpressionProperty>>();
        
        /// <summary>
        /// All foreign key properties per type
        /// </summary>
        internal static Dictionary<Type, List<ExpressionProperty>> FKProperties = new Dictionary<Type, List<ExpressionProperty>>();
        
        /// <summary>
        /// All OneToMany properties per type
        /// </summary>
        internal static Dictionary<Type, List<ExpressionProperty>> OneToManyProperties = new Dictionary<Type, List<ExpressionProperty>>();
      
        /// <summary>
        /// All List properties per type
        /// </summary>
        internal static Dictionary<Type, List<ExpressionProperty>> GemListProperties = new Dictionary<Type, List<ExpressionProperty>>();
    
        /// <summary>
        /// All ManyToMany properties per type
        /// </summary>
        internal static Dictionary<Type, List<ExpressionProperty>> ManyToManyProperties = new Dictionary<Type, List<ExpressionProperty>>();

        /// <summary>
        /// A list of projection properties per type
        /// </summary>
        internal static Dictionary<Type, List<ExpressionProperty>> ProjectionProperties = new Dictionary<Type, List<ExpressionProperty>>();

        /// <summary>
        /// Pre composed order by clauses per type
        /// </summary>
        internal static Dictionary<Type, string> OrderBySql = new Dictionary<Type, string>();
        
        /// <summary>
        /// The list of post-"compiled" expressions used to deflect reflection for Dwarf.Load
        /// </summary>
        internal static Dictionary<Type, Func<Guid, object>> LoadExpressions = new Dictionary<Type, Func<Guid, object>>();

        /// <summary>
        /// The list of post-"compiled" expressions used to deflect reflection for Gem.Load
        /// </summary>
        internal static Dictionary<Type, Func<string, object>> LoadGem = new Dictionary<Type, Func<string, object>>();

        /// <summary>
        /// The list of post-"compiled" expressions used to deflect reflection 
        /// </summary>
        internal static Dictionary<Type, Dictionary<string, ExpressionProperty>> PropertyExpressions = new Dictionary<Type, Dictionary<string, ExpressionProperty>>();

        /// <summary>
        /// A list of connection strings per assembly
        /// </summary>
        internal static Dictionary<Assembly, string> ConnectionString = new Dictionary<Assembly, string>();

        /// <summary>
        /// Gets or Sets the database for the domain model
        /// </summary>
        internal static Dictionary<Assembly, IDatabase> Databases = new Dictionary<Assembly, IDatabase>();

        /// <summary>
        /// Gets or Sets the database's query constructor for the domain model
        /// </summary>
        internal static Dictionary<Assembly, IQueryConstructor> QueryConstructors = new Dictionary<Assembly, IQueryConstructor>();

        /// <summary>
        /// The list of proxy types constructor functions
        /// </summary>
        internal static Dictionary<Type, Func<object>> ProxyTypeConstructors = new Dictionary<Type, Func<object>>();
    }
}
